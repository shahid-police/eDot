using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Application.Customers;
using Elegance.Application.Orders;
using Elegance.Domain.Customers;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminCustomerService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminCustomerService
{
    public async Task<PagedResult<AdminCustomerSummaryDto>> ListCustomersAsync(AdminCustomerQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var customers = dbContext.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            customers = customers.Where(c => c.Email.Contains(search) || c.FirstName.Contains(search) || c.LastName.Contains(search));
        }

        var total = await customers.CountAsync(cancellationToken);
        var items = await customers
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCustomerSummaryDto
            {
                Id = c.Id,
                Email = c.Email,
                FirstName = c.FirstName,
                LastName = c.LastName,
                IsActive = c.IsActive,
                OrderCount = dbContext.Orders.Count(o => o.CustomerId == c.Id),
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminCustomerSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<AdminCustomerDetailDto> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw NotFound();

        var orderCount = await dbContext.Orders.CountAsync(o => o.CustomerId == customerId, cancellationToken);

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
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

        return new AdminCustomerDetailDto
        {
            Customer = ToSummary(customer, orderCount),
            Addresses = customer.Addresses
                .OrderBy(a => a.Id)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    Label = a.Label,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    Province = a.Province,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefaultShipping = a.IsDefaultShipping,
                    IsDefaultBilling = a.IsDefaultBilling,
                }).ToList(),
            Orders = orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                GrandTotal = o.GrandTotal,
                Currency = o.Currency,
                ItemCount = o.ItemCount,
                CreatedAt = o.CreatedAt,
            }).ToList(),
        };
    }

    public async Task<AdminCustomerSummaryDto> SetCustomerActiveAsync(AdminActor actor, long customerId, bool isActive, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw NotFound();

        var now = DateTime.UtcNow;
        customer.IsActive = isActive;
        customer.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        // Deactivation ends existing sessions now. Access tokens already issued expire within
        // the access token lifetime, and the customer cannot refresh them.
        if (!isActive)
        {
            await dbContext.RefreshTokens
                .Where(t => t.CustomerId == customerId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTime?)now), cancellationToken);
        }

        await auditLog.RecordAsync(
            actor, isActive ? "customer.activate" : "customer.deactivate", "Customer", customerId.ToString(),
            customer.Email, cancellationToken);

        return (await GetCustomerAsync(customerId, cancellationToken)).Customer;
    }

    private static AdminCustomerSummaryDto ToSummary(Customer customer, int orderCount) => new()
    {
        Id = customer.Id,
        Email = customer.Email,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        IsActive = customer.IsActive,
        OrderCount = orderCount,
        CreatedAt = customer.CreatedAt,
    };

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
