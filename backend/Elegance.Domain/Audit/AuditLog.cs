namespace Elegance.Domain.Audit;

public class AuditLog
{
    public long Id { get; set; }
    public long? AdminUserId { get; set; }
    public required string AdminEmail { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public required string Summary { get; set; }
    public DateTime CreatedAt { get; set; }
}
