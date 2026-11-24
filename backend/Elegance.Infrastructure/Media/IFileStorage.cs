namespace Elegance.Infrastructure.Media;

public record StoredFile(string StorageKey, string Url);

public interface IFileStorage
{
    Task<StoredFile> PutAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    // Reads a previously-stored file back. Used for admin review/download of vendor-original
    // images and to re-stream a vendor's original bytes into the existing public upload pipeline
    // when an admin chooses to publish one (see AdminVendorSubmissionsController).
    Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default);
}
