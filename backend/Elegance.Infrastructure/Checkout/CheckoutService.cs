using Elegance.Application.Checkout;
using Elegance.Application.Common;
using Elegance.Application.Customers;
using Elegance.Application.Orders;
using Elegance.Application.Payments;
using Elegance.Domain.Carts;
using Elegance.Domain.Catalog;
using Elegance.Domain.Customers;
using Elegance.Domain.Discounts.Enums;
using Elegance.Domain.Inventory;
using Elegance.Domain.Marketing;
using Elegance.Domain.Marketing.Enums;
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

    private sealed record CartLine(CartItem Item, Product Product, ProductVariant? Variant, decimal OriginalUnitPrice, decimal UnitPrice)
    {
        public int Quantity => Item.Quantity;
        public decimal LineTotal => UnitPrice * Quantity;
        public decimal ProductDiscountTotal => (OriginalUnitPrice - UnitPrice) * Quantity;
    }

    // Everything PlaceOrderAsync needs to snapshot onto the Order once a promo code evaluates
    // successfully against the cart's current contents.
    private sealed record PromoCodeEvaluation(
        PromoCode Code, decimal DiscountAmount, decimal? DiscountPercentageSnapshot,
        decimal? CommissionPercentage, decimal CommissionEligibleAmount);

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
        var (cart, lines) = await LoadCartAsync(owner, cancellationToken);
        var subtotal = lines.Sum(l => l.LineTotal);
        var totals = CalculateTotals(subtotal);

        // A promo code applied earlier can stop being valid just by the cart or the calendar
        // moving on (expired, no longer covers what's left in the cart, hit its usage limit...).
        // Viewing the cart must never break over that - just stop showing the discount until the
        // customer removes it or fixes their cart. PlaceOrderAsync re-validates for real and does
        // throw, since that's the moment an invalid code actually has to be rejected.
        PromoCodeEvaluation? evaluation = null;
        if (cart?.AppliedPromoCodeId is { } appliedId)
        {
            try
            {
                var appliedCode = await FindPromoCodeByIdAsync(appliedId, cancellationToken);
                if (appliedCode is not null)
                {
                    evaluation = await EvaluatePromoCodeOrThrowAsync(appliedCode, lines, owner.CustomerId, cancellationToken);
                }
            }
            catch (ApplicationFailure)
            {
            }
        }

        var promoDiscount = evaluation?.DiscountAmount ?? 0;
        return new CheckoutSummaryDto
        {
            IsEmpty = lines.Count == 0,
            ItemCount = lines.Sum(l => l.Quantity),
            Subtotal = subtotal,
            ShippingTotal = totals.Shipping,
            TaxTotal = totals.Tax,
            AppliedPromoCode = evaluation?.Code.Code,
            PromoCodeDiscountAmount = promoDiscount,
            GrandTotal = Math.Max(0, totals.Grand - promoDiscount),
            Currency = _options.Currency,
            FreeShippingThreshold = _options.FreeShippingThreshold,
        };
    }

    public async Task<CheckoutSummaryDto> ApplyPromoCodeAsync(ListOwner owner, ApplyPromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(owner, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.Validation, "Your cart is empty.");

        var (_, lines) = await LoadCartAsync(owner, cancellationToken);
        if (lines.Count == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Your cart is empty.");
        }

        var code = await FindPromoCodeAsync(request.Code, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "That promo code could not be found.");

        // Evaluate before attaching, so an invalid code is rejected without ever being saved onto
        // the cart - the customer sees exactly why, and nothing changes if it fails.
        await EvaluatePromoCodeOrThrowAsync(code, lines, owner.CustomerId, cancellationToken);

        cart.AppliedPromoCodeId = code.Id;
        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetSummaryAsync(owner, cancellationToken);
    }

    public async Task<CheckoutSummaryDto> RemovePromoCodeAsync(ListOwner owner, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(owner, cancellationToken);
        if (cart is not null && cart.AppliedPromoCodeId is not null)
        {
            cart.AppliedPromoCodeId = null;
            cart.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetSummaryAsync(owner, cancellationToken);
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
        var productDiscountTotal = lines.Sum(l => l.ProductDiscountTotal);
        var now = DateTime.UtcNow;

        // Re-evaluated from scratch against the cart as it stands right now - never trusted from
        // whatever ApplyPromoCodeAsync computed earlier. An invalid code rejects the whole order
        // (not silently dropped), so the customer isn't charged something they didn't agree to.
        PromoCodeEvaluation? promoEvaluation = null;
        if (cart.AppliedPromoCodeId is { } appliedPromoCodeId)
        {
            var appliedCode = await FindPromoCodeByIdAsync(appliedPromoCodeId, cancellationToken)
                ?? throw new ApplicationFailure(FailureKind.Validation, "Your promo code is no longer valid. Please remove it and try again.");
            promoEvaluation = await EvaluatePromoCodeOrThrowAsync(appliedCode, lines, owner.CustomerId, cancellationToken);
        }

        var promoDiscountAmount = promoEvaluation?.DiscountAmount ?? 0m;
        var grandTotal = Math.Max(0, totals.Grand - promoDiscountAmount);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var line in lines)
        {
            await TakeStockAsync(line, cancellationToken);
        }

        if (promoEvaluation is not null)
        {
            await ConsumePromoCodeUsageAsync(promoEvaluation.Code.Id, cancellationToken);
        }

        var order = new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"),
            CustomerId = customer?.Id,
            GuestTokenHash = customer is null && owner.GuestToken is not null ? TokenService.HashToken(owner.GuestToken) : null,
            Email = email,
            Phone = shipping.Phone,
            Subtotal = subtotal,
            ProductDiscountTotal = productDiscountTotal,
            ShippingTotal = totals.Shipping,
            TaxTotal = totals.Tax,
            PromoCodeId = promoEvaluation?.Code.Id,
            PromoterId = promoEvaluation?.Code.PromoterId,
            PromoCodeDiscountPercentageSnapshot = promoEvaluation?.DiscountPercentageSnapshot,
            PromoCodeDiscountAmount = promoDiscountAmount,
            CommissionPercentageSnapshot = promoEvaluation?.CommissionPercentage,
            CommissionEligibleAmount = promoEvaluation?.CommissionEligibleAmount,
            GrandTotal = grandTotal,
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
                OriginalUnitPrice = l.OriginalUnitPrice,
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
                    Amount = grandTotal,
                    Currency = _options.Currency,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
            ],
        };

        dbContext.Orders.Add(order);

        if (promoEvaluation is not null)
        {
            dbContext.PromoCodeUsages.Add(new PromoCodeUsage
            {
                PromoCode = promoEvaluation.Code,
                PromoterId = promoEvaluation.Code.PromoterId,
                CustomerId = customer?.Id,
                Order = order,
                DiscountAmount = promoDiscountAmount,
                UsedAt = now,
            });

            if (promoEvaluation.CommissionPercentage is { } commissionPercentage)
            {
                var commissionAmount = Math.Round(
                    promoEvaluation.CommissionEligibleAmount * commissionPercentage / 100m, 2, MidpointRounding.AwayFromZero);

                dbContext.PromoterCommissions.Add(new PromoterCommission
                {
                    PromoterId = promoEvaluation.Code.PromoterId,
                    PromoCode = promoEvaluation.Code,
                    Order = order,
                    CommissionPercentageSnapshot = commissionPercentage,
                    EligibleSaleAmount = promoEvaluation.CommissionEligibleAmount,
                    CommissionAmount = commissionAmount,
                    Currency = _options.Currency,
                    Status = CommissionStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

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
        // The code was just consumed by this order - clearing it here (rather than leaving it on
        // the cart) means the next time this cart is used, no stale code is silently re-applied.
        cart.AppliedPromoCodeId = null;
        cart.UpdatedAt = now;
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

        var now = DateTime.UtcNow;
        var items = await dbContext.CartItems
            .Where(i => i.CartId == cart.Id)
            .Include(i => i.Product).ThenInclude(p => p.Discounts.Where(d => d.Status == ProductDiscountStatus.Active && d.StartDate <= now && d.EndDate >= now))
            .Include(i => i.Variant).ThenInclude(v => v!.Discounts.Where(d => d.Status == ProductDiscountStatus.Active && d.StartDate <= now && d.EndDate >= now))
            .OrderBy(i => i.AddedAt)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var lines = items
            .Where(i => i.Product.IsActive && (i.Variant is null || i.Variant.IsActive))
            .Select(i => new CartLine(i, i.Product, i.Variant, PricingRules.NormalPrice(i.Product, i.Variant), PricingRules.UnitPrice(i.Product, i.Variant)))
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

    private Task<PromoCode?> FindPromoCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.PromoCodes
            .Include(c => c.Promoter)
            .Include(c => c.Products)
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Code == code.Trim(), cancellationToken);

    private Task<PromoCode?> FindPromoCodeByIdAsync(long promoCodeId, CancellationToken cancellationToken) =>
        dbContext.PromoCodes
            .Include(c => c.Promoter)
            .Include(c => c.Products)
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Id == promoCodeId, cancellationToken);

    // The one place that decides whether a promo code may be used against a given cart right now,
    // and what it's worth. Always throws ApplicationFailure (never returns a failure in-band) -
    // ApplyPromoCodeAsync and PlaceOrderAsync let that propagate (an invalid code must block the
    // action it's taken on); GetSummaryAsync is the only caller that swallows it, since merely
    // viewing the cart must never break over a code that quietly stopped being valid.
    private async Task<PromoCodeEvaluation> EvaluatePromoCodeOrThrowAsync(
        PromoCode code, List<CartLine> lines, long? customerId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (code.Status != PromoCodeStatus.Active)
        {
            throw new ApplicationFailure(FailureKind.Validation, "This promo code is no longer active.");
        }
        if (now < code.StartDate)
        {
            throw new ApplicationFailure(FailureKind.Validation, "This promo code isn't active yet.");
        }
        if (code.ExpiryDate is { } expiry && now > expiry)
        {
            throw new ApplicationFailure(FailureKind.Validation, "This promo code has expired.");
        }
        if (code.Promoter.Status != PromoterStatus.Active)
        {
            throw new ApplicationFailure(FailureKind.Validation, "This promo code is currently unavailable.");
        }
        if (code.MaximumTotalUses is { } maxTotal && code.UsedCount >= maxTotal)
        {
            throw new ApplicationFailure(FailureKind.Validation, "This promo code has reached its usage limit.");
        }

        var subtotal = lines.Sum(l => l.LineTotal);
        if (code.MinimumOrderAmount is { } minOrder && subtotal < minOrder)
        {
            throw new ApplicationFailure(FailureKind.Validation, $"This promo code needs an order of at least {minOrder:N0} {_options.Currency}.");
        }

        var hasRestriction = code.Products.Count > 0 || code.Categories.Count > 0;
        var productIds = code.Products.Select(p => p.ProductId).ToHashSet();
        var categoryIds = code.Categories.Select(c => c.CategoryId).ToHashSet();
        var eligibleLines = hasRestriction
            ? lines.Where(l => productIds.Contains(l.Product.Id) || categoryIds.Contains(l.Product.CategoryId)).ToList()
            : lines;

        if (hasRestriction && eligibleLines.Count == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "This promo code doesn't apply to anything in your cart.");
        }

        if (customerId is { } id && code.MaximumUsesPerCustomer is { } maxPerCustomer)
        {
            var usedByCustomer = await dbContext.PromoCodeUsages
                .CountAsync(u => u.PromoCodeId == code.Id && u.CustomerId == id, cancellationToken);
            if (usedByCustomer >= maxPerCustomer)
            {
                throw new ApplicationFailure(FailureKind.Validation, "You've already used this promo code the maximum number of times.");
            }
        }

        var eligibleSubtotal = eligibleLines.Sum(l => l.LineTotal);
        var rawDiscount = code.GrantsCustomerDiscount ? eligibleSubtotal * code.CustomerDiscountPercentage / 100m : 0m;
        var cappedDiscount = code.MaximumDiscountAmount is { } maxDiscount ? Math.Min(rawDiscount, maxDiscount) : rawDiscount;
        var discountAmount = Math.Round(cappedDiscount, 2, MidpointRounding.AwayFromZero);

        var commissionPercentage = code.GrantsCommission ? code.CommissionPercentageOverride ?? code.Promoter.CommissionPercentage : (decimal?)null;
        var commissionEligibleAmount = eligibleSubtotal - discountAmount;

        return new PromoCodeEvaluation(
            code, discountAmount,
            code.GrantsCustomerDiscount ? code.CustomerDiscountPercentage : null,
            commissionPercentage, commissionEligibleAmount);
    }

    // Same conditional-update shape as TakeStockAsync below: two checkouts racing to use the last
    // remaining use of a capped promo code cannot both succeed, since only one row update matches
    // a UsedCount that still has room.
    private async Task ConsumePromoCodeUsageAsync(long promoCodeId, CancellationToken cancellationToken)
    {
        var taken = await dbContext.PromoCodes
            .Where(c => c.Id == promoCodeId && (c.MaximumTotalUses == null || c.UsedCount < c.MaximumTotalUses))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1), cancellationToken);

        if (taken == 0)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This promo code just reached its usage limit. Please remove it and try again.");
        }
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
