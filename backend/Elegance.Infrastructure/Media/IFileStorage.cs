namespace Elegance.Infrastructure.Media;

public record StoredFile(string StorageKey, string Url);

public interface IFileStorage
{
    Task<StoredFile> PutAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
