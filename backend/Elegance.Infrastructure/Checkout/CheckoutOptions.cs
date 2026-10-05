namespace Elegance.Infrastructure.Checkout;

public class CheckoutOptions
{
    public const string SectionName = "Checkout";

    public string Currency { get; set; } = "PKR";
    public decimal FlatShippingFee { get; set; } = 350m;
    public decimal FreeShippingThreshold { get; set; } = 10000m;
    public decimal TaxRatePercent { get; set; }
    public BankTransferOptions BankTransfer { get; set; } = new();
}

public class BankTransferOptions
{
    public string AccountTitle { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
}
