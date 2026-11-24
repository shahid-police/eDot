using System.Collections.Concurrent;
using Elegance.Infrastructure.Media;

namespace Elegance.Tests.TestSupport;

// In-memory IFileStorage double - no real disk/S3 I/O, so tests of upload/publish flows can run
// fast and deterministically while still exercising a real PutAsync -> GetAsync round trip.
public sealed class FakeFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public Task<StoredFile> PutAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        _files[storageKey] = buffer.ToArray();
        return Task.FromResult(new StoredFile(storageKey, $"https://test.local/{storageKey}"));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _files.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }

    public Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(storageKey, out var bytes))
        {
            throw new FileNotFoundException(storageKey);
        }
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public bool Contains(string storageKey) => _files.ContainsKey(storageKey);
}
