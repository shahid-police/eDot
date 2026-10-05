using Elegance.Application.Payments;
using Elegance.Domain.Orders.Enums;

namespace Elegance.Infrastructure.Payments;

public class CashOnDeliveryGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.CashOnDelivery;

    public PaymentInstructions Describe(string orderNumber, decimal amount, string currency) =>
        new(
            "Cash on delivery",
            $"Pay {amount:N2} {currency} in cash when your order arrives. Order {orderNumber} will be confirmed by phone before dispatch.");
}
