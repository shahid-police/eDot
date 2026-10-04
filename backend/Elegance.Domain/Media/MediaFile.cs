namespace Elegance.Domain.Media;

public class MediaFile
{
    public long Id { get; set; }
    public required string StorageKey { get; set; }
    public required string Url { get; set; }
    public string? CdnUrl { get; set; }
    public string? OriginalFilename { get; set; }
    public string? MimeType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? AltText { get; set; }
    public DateTime CreatedAt { get; set; }
}
