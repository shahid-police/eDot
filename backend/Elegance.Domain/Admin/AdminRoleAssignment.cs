namespace Elegance.Domain.Admin;

public class AdminRoleAssignment
{
    public long AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;

    public AdminRole Role { get; set; }
}
