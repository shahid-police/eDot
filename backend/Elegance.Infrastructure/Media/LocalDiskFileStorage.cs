using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Media;

// Development only. Files live under {ContentRoot}/wwwroot/uploads and Program.cs serves that
// folder with UseStaticFiles(). Nothing here survives a redeploy to a fresh container, and
// nothing here is shared across multiple API instances - that's what the R2 provider is for.
public class LocalDiskFileStorage(IHostEnvironment environment, IOptions<StorageOptions> options) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    private string RootPath => Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");

    public async Task<StoredFile> PutAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(RootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/uploads/{storageKey}";
        return new StoredFile(storageKey, url);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(RootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
}
