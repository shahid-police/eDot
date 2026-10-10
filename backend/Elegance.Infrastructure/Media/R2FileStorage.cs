using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Media;

// Cloudflare R2 speaks the S3 API, so the official AWS SDK works against it unmodified once
// pointed at the account's R2 endpoint. Credentials and the bucket name come from Storage:R2
// (never commit real values - set them via environment variables in production).
public class R2FileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    private AmazonS3Client CreateClient()
    {
        var r2 = _options.R2;
        return new AmazonS3Client(r2.AccessKey, r2.SecretKey, new AmazonS3Config
        {
            ServiceURL = $"https://{r2.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
        });
    }

    public async Task<StoredFile> PutAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.R2.Bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
        }, cancellationToken);

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/{storageKey}";
        return new StoredFile(storageKey, url);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        await client.DeleteObjectAsync(_options.R2.Bucket, storageKey, cancellationToken);
    }
}
