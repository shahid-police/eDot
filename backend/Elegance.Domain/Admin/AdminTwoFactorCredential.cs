namespace Elegance.Domain.Admin;

public enum AdminTwoFactorStatus
{
    Pending,
    Active,
}

// One-to-one with AdminUser. Holds the Data-Protection-encrypted TOTP secret, never the
// plaintext secret. LastAcceptedTimeStep blocks replay of an already-used code.
public class AdminTwoFactorCredential
{
    public long AdminUserId { get; set; }
    public required string EncryptedSecret { get; set; }
    public AdminTwoFactorStatus Status { get; set; } = AdminTwoFactorStatus.Pending;
    public long LastAcceptedTimeStep { get; set; } = -1;
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PendingExpiresAt { get; set; }

    public AdminUser AdminUser { get; set; } = null!;
}
