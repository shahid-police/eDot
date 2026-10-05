using Elegance.Application.Checkout;
using Elegance.Application.Common;
using Elegance.Application.Customers;
using Elegance.Application.Orders;
using Elegance.Application.Payments;
using Elegance.Domain.Carts;
using Elegance.Domain.Catalog;
using Elegance.Domain.Customers;
using Elegance.Domain.Inventory;
using Elegance.Domain.Orders;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Catalog;
using Elegance.Infrastructure.Orders;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Checkout;

public class CheckoutService(
    EleganceDbContext dbContext,
    IOptions<CheckoutOptions> options,
    IEnumerable<IPaymentGateway> gateways,
    OrderDetailBuilder detailBuilder) : ICheckoutService
{
    private readonly CheckoutOptions _options = options.Value;

    private sealed record CartLine(CartItem Item, Product Product, ProductVariant? Variant, decimal UnitPrice)
    {
        public int Quantity => Item.Quantity;
        public decimal LineTotal => UnitPrice * Quantity;
    }

    private sealed record ShippingSnapshot(
        string FullName,
        string Phone,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string Province,
        string? PostalCode,
        string Country);

    public async Task<CheckoutSummaryDto> GetSummaryAsync(ListOwner owner, CancellationToken cancellationToken = default)
    {
        var (_, lines) = await LoadCartAsync(owner, cancellationToken);
        var subtotal = lines.Sum(l => l.LineTotal);
        var totals = CalculateTotals(subtotal);

        return new CheckoutSummaryDto
        {
            IsEmpty = lines.Count == 0,
            ItemCount = lines.Sum(l => l.Quantity),
            Subtotal = subtotal,
            ShippingTotal = totals.Shipping,
            TaxTotal = totals.Tax,
            GrandTotal = totals.Grand,
            Currency = _options.Currency,
            FreeShippingThreshold = _options.FreeShippingThreshold,
        };
    }

    public async Task<OrderDetailDto> PlaceOrderAsync(ListOwner owner, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var method = ParsePaymentMethod(request.PaymentMethod);
        if (!gateways.Any(g => g.Method == method))
        {
            throw new ApplicationFailure(FailureKind.Validation, "This payment method is not available.");
        }

        var customer = owner.CustomerId is { } customerId
            ? await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            : null;

        var (cart, lines) = await LoadCartAsync(owner, cancellationToken);
        if (cart is null || lines.Count == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Your cart is empty.");
        }

        var shipping = await ResolveShippingAsync(customer, request, cancellationToken);
        var email = customer?.Email ?? NormalizeEmail(request.Email)
            ?? throw new ApplicationFailure(FailureKind.Validation, "Please enter an email address for your order.");

        var subtotal = lines.Sum(l => l.LineTotal);
        var totals = CalculateTotals(subtotal);
        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var line in lines)
        {
            await TakeStockAsync(line, cancellationToken);
        }

        var order = new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"),
            CustomerId = customer?.Id,
            GuestTokenHash = customer is null && owner.GuestToken is not null ? TokenService.HashToken(owner.GuestToken) : null,
            Email = email,
            Phone = shipping.Phone,
            Subtotal = subtotal,
            ShippingTotal = totals.Shipping,
            TaxTotal = totals.Tax,
            GrandTotal = totals.Grand,
            Currency = _options.Currency,
            Status = OrderStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Items = lines.Select(l => new OrderItem
            {
                ProductId = l.Product.Id,
                VariantId = l.Variant?.Id,
                Sku = l.Variant?.Sku ?? l.Product.Sku,
                ProductName = l.Product.Name,
                VariantName = l.Variant?.VariantName,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                LineTotal = l.LineTotal,
            }).ToList(),
            Addresses =
            [
                new OrderAddress
                {
                    Type = AddressType.Shipping,
                    FullName = shipping.FullName,
                    Phone = shipping.Phone,
                    AddressLine1 = shipping.AddressLine1,
                    AddressLine2 = shipping.AddressLine2,
                    City = shipping.City,
                    Province = shipping.Province,
                    PostalCode = shipping.PostalCode,
                    Country = shipping.Country,
                },
            ],
            StatusHistory =
            [
                new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Order placed", CreatedAt = now },
            ],
            Payments =
            [
                new Payment
                {
                    Method = method,
                    Status = PaymentStatus.Pending,
                    Amount = totals.Grand,
                    Currency = _options.Currency,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
            ],
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.StockMovements.AddRange(lines.Select(l => new StockMovement
        {
            ProductId = l.Product.Id,
            VariantId = l.Variant?.Id,
            QuantityDelta = -l.Quantity,
            Reason = StockMovementReason.Sale,
            OrderId = order.Id,
            CreatedAt = now,
        }));

        order.OrderNumber = $"E-{now:yyyy}-{order.Id:D6}";
        await dbContext.CartItems
            .Where(i => i.CartId == cart.Id)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await BuildDetailAsync(order.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(ListOwner owner, CancellationToken cancellationToken = default)
    {
        var orders = await OwnedOrders(owner)
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Take(50)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.GrandTotal,
                o.Currency,
                o.CreatedAt,
                ItemCount = o.Items.Sum(i => i.Quantity),
            })
            .ToListAsync(cancellationToken);

        return orders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            GrandTotal = o.GrandTotal,
            Currency = o.Currency,
            ItemCount = o.ItemCount,
            CreatedAt = o.CreatedAt,
        }).ToList();
    }

    public async Task<OrderDetailDto> GetOrderAsync(ListOwner owner, long orderId, CancellationToken cancellationToken = default)
    {
        var id = await OwnedOrders(owner).Where(o => o.Id == orderId).Select(o => (long?)o.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested order was not found.");
        return await BuildDetailAsync(id, cancellationToken);
    }

    public async Task<OrderDetailDto> GetOrderByNumberAsync(ListOwner owner, string orderNumber, CancellationToken cancellationToken = default)
    {
        var id = await OwnedOrders(owner).Where(o => o.OrderNumber == orderNumber).Select(o => (long?)o.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested order was not found.");
        return await BuildDetailAsync(id, cancellationToken);
    }

    public async Task AdoptGuestOrdersAsync(long customerId, string guestToken, CancellationToken cancellationToken = default)
    {
        var hash = TokenService.HashToken(guestToken);
        await dbContext.Orders
            .Where(o => o.GuestTokenHash == hash && o.CustomerId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.CustomerId, customerId).SetProperty(o => o.GuestTokenHash, (string?)null), cancellationToken);
    }

    private IQueryable<Order> OwnedOrders(ListOwner owner)
    {
        if (owner.CustomerId is { } customerId)
        {
            return dbContext.Orders.Where(o => o.CustomerId == customerId);
        }

        if (!string.IsNullOrEmpty(owner.GuestToken))
        {
            var hash = TokenService.HashToken(owner.GuestToken);
            return dbContext.Orders.Where(o => o.GuestTokenHash == hash);
        }

        return dbContext.Orders.Where(o => false);
    }

    private async Task<(Cart? Cart, List<CartLine> Lines)> LoadCartAsync(ListOwner owner, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(owner, cancellationToken);
        if (cart is null)
        {
            return (null, []);
        }

        var items = await dbContext.CartItems
            .Where(i => i.CartId == cart.Id)
            .Include(i => i.Product)
            .Include(i => i.Variant)
            .OrderBy(i => i.AddedAt)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var lines = items
            .Where(i => i.Product.IsActive && (i.Variant is null || i.Variant.IsActive))
            .Select(i => new CartLine(i, i.Product, i.Variant, PricingRules.UnitPrice(i.Product, i.Variant)))
            .ToList();

        if (lines.Count != items.Count)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Some items in your cart are no longer available. Please review your cart.");
        }

        return (cart, lines);
    }

    private Task<Cart?> FindCartAsync(ListOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is { } customerId)
        {
            return dbContext.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(owner.GuestToken))
        {
            var hash = TokenService.HashToken(owner.GuestToken);
            return dbContext.Carts.FirstOrDefaultAsync(c => c.GuestTokenHash == hash, cancellationToken);
        }

        return Task.FromResult<Cart?>(null);
    }

    // The conditional update is the stock check: two checkouts racing for the last unit
    // cannot both succeed, because only one row update matches a stock count that covers it.
    private async Task TakeStockAsync(CartLine line, CancellationToken cancellationToken)
    {
        var quantity = line.Quantity;

        var taken = line.Variant is null
            ? await dbContext.Products
                .Where(p => p.Id == line.Product.Id && p.IsActive && p.StockQuantity >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity), cancellationToken)
            : await dbContext.ProductVariants
                .Where(v => v.Id == line.Variant.Id && v.IsActive && v.StockQuantity >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - quantity), cancellationToken);

        if (taken == 0)
        {
            throw new ApplicationFailure(
                FailureKind.Conflict,
                $"{line.Product.Name} no longer has {quantity} available. Please update your cart and try again.");
        }
    }

    private async Task<ShippingSnapshot> ResolveShippingAsync(Customer? customer, CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (request.ShippingAddressId is { } addressId)
        {
            if (customer is null)
            {
                throw new ApplicationFailure(FailureKind.Validation, "Sign in to use a saved address.");
            }

            var saved = await dbContext.CustomerAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customer.Id, cancellationToken)
                ?? throw new ApplicationFailure(FailureKind.NotFound, "The selected address was not found.");

            return new ShippingSnapshot(
                saved.FullName, saved.Phone, saved.AddressLine1, saved.AddressLine2,
                saved.City, saved.Province, saved.PostalCode, saved.Country);
        }

        var input = request.ShippingAddress
            ?? throw new ApplicationFailure(FailureKind.Validation, "Please provide a shipping address.");

        return new ShippingSnapshot(
            input.FullName.Trim(),
            input.Phone.Trim(),
            input.AddressLine1.Trim(),
            Trimmed(input.AddressLine2),
            input.City.Trim(),
            input.Province.Trim(),
            Trimmed(input.PostalCode),
            string.IsNullOrWhiteSpace(input.Country) ? "PK" : input.Country.Trim().ToUpperInvariant());
    }

    private (decimal Shipping, decimal Tax, decimal Grand) CalculateTotals(decimal subtotal)
    {
        var shipping = subtotal == 0 || subtotal >= _options.FreeShippingThreshold ? 0m : _options.FlatShippingFee;
        var tax = Math.Round(subtotal * _options.TaxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        return (shipping, tax, subtotal + shipping + tax);
    }

    private Task<OrderDetailDto> BuildDetailAsync(long orderId, CancellationToken cancellationToken) =>
        detailBuilder.BuildAsync(orderId, cancellationToken);
    private static PaymentMethod ParsePaymentMethod(string value) =>
        Enum.TryParse<PaymentMethod>(value, ignoreCase: true, out var method) && Enum.IsDefined(method)
            ? method
            : throw new ApplicationFailure(FailureKind.Validation, "Please choose a payment method.");

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
