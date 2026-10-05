using Elegance.Application.Orders;
using Elegance.Application.Payments;
using Elegance.Domain.Orders;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Orders;

public class OrderDetailBuilder(EleganceDbContext dbContext, IEnumerable<IPaymentGateway> gateways)
{
    public async Task<OrderDetailDto> BuildAsync(long orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Addresses)
            .Include(o => o.StatusHistory)
            .Include(o => o.Payments)
            .FirstAsync(o => o.Id == orderId, cancellationToken);

        var address = order.Addresses.First(a => a.Type == AddressType.Shipping);
        var gatewayByMethod = gateways.ToDictionary(g => g.Method);

        return new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Email = order.Email,
            Phone = order.Phone,
            Subtotal = order.Subtotal,
            ShippingTotal = order.ShippingTotal,
            TaxTotal = order.TaxTotal,
            GrandTotal = order.GrandTotal,
            Currency = order.Currency,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            ShippingAddress = new OrderAddressDto
            {
                FullName = address.FullName,
                Phone = address.Phone,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Country = address.Country,
            },
            Items = order.Items.OrderBy(i => i.Id).Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                Sku = i.Sku,
                ProductName = i.ProductName,
                VariantName = i.VariantName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal,
            }).ToList(),
            Payments = order.Payments.Select(p => new OrderPaymentDto
            {
                Method = p.Method.ToString(),
                Status = p.Status.ToString(),
                Amount = p.Amount,
                Currency = p.Currency,
                Instructions = gatewayByMethod[p.Method].Describe(order.OrderNumber, p.Amount, p.Currency),
            }).ToList(),
            History = order.StatusHistory.OrderBy(h => h.Id).Select(h => new OrderStatusEntryDto
            {
                Status = h.Status.ToString(),
                Note = h.Note,
                CreatedAt = h.CreatedAt,
            }).ToList(),
        };
    }

}
