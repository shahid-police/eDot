namespace Elegance.Infrastructure.Marketing;

public class CommissionOptions
{
    public const string SectionName = "Commission";

    // How long after an order reaches Delivered before its commission is safe to pay out -
    // matches the spec's own suggested eligibility rule ("after successful delivery and
    // completion of the applicable return period").
    public int ReturnWindowDays { get; set; } = 7;

    public int PollIntervalSeconds { get; set; } = 3600;
}
