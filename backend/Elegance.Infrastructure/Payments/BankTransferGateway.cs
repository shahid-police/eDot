using Elegance.Application.Payments;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Checkout;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Payments;

public class BankTransferGateway(IOptions<CheckoutOptions> options) : IPaymentGateway
{
    private readonly BankTransferOptions _bank = options.Value.BankTransfer;

    public PaymentMethod Method => PaymentMethod.BankTransfer;

    public PaymentInstructions Describe(string orderNumber, decimal amount, string currency)
    {
        var details = string.Join(", ", new[]
        {
            Labelled("Account title", _bank.AccountTitle),
            Labelled("Bank", _bank.BankName),
            Labelled("Account number", _bank.AccountNumber),
            Labelled("IBAN", _bank.Iban),
        }.Where(part => part is not null));

        var message = details.Length == 0
            ? $"Transfer {amount:N2} {currency} and quote {orderNumber}. Our team will send the bank details by email."
            : $"Transfer {amount:N2} {currency} to {details}. Quote {orderNumber} as the payment reference.";

        return new PaymentInstructions("Bank transfer", message);
    }

    private static string? Labelled(string label, string value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label} {value}";
}
