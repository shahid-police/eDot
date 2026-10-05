using Elegance.Domain.Orders.Enums;

namespace Elegance.Application.Payments;

public interface IPaymentGateway
{
    PaymentMethod Method { get; }

    PaymentInstructions Describe(string orderNumber, decimal amount, string currency);
}

public sealed record PaymentInstructions(string Title, string Message);
