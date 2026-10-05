using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Audit;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AuditLogService(EleganceDbContext dbContext) : IAuditLogService
{
    public async Task RecordAsync(AdminActor actor, string action, string entityType, string? entityId, string summary, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            AdminUserId = actor.Id,
            AdminEmail = actor.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = summary.Length > 1000 ? summary[..1000] : summary,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.AuditLogs.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(a => new AuditLogDto
            {
                Id = a.Id,
                AdminEmail = a.AdminEmail,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Summary = a.Summary,
                CreatedAt = a.CreatedAt,
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
