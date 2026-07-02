using System.Text.Json;
using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Common;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fixar.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Runs on every SaveChanges call. Stamps Created/LastModified metadata on
/// <see cref="BaseAuditableEntity"/> instances and writes an
/// <see cref="AuditLog"/> row for every tracked insert/update/delete,
/// added to the same change set so it commits atomically with the change
/// it describes.
/// </summary>
public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AuditableEntitySaveChangesInterceptor(
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext context)
    {
        var utcNow = _dateTimeService.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Created = utcNow;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.LastModified = utcNow;
                entry.Entity.LastModifiedBy = userId;
            }
        }

        var auditEntries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity is not AuditLog &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditEntries.Count == 0)
        {
            return;
        }

        foreach (var entry in auditEntries)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = _currentUserService.Email,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Timestamp = utcNow,
                IpAddress = _currentUserService.IpAddress,
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.Create,
                    EntityState.Deleted => AuditAction.Delete,
                    _ => AuditAction.Update
                }
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var affectedColumns = new List<string>();

            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            affectedColumns.Add(propertyName);
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                        }

                        break;
                }
            }

            if (entry.State == EntityState.Modified && affectedColumns.Count == 0)
            {
                continue;
            }

            auditLog.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
            auditLog.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;
            auditLog.AffectedColumns = affectedColumns.Count > 0 ? string.Join(",", affectedColumns) : null;

            context.Set<AuditLog>().Add(auditLog);
        }
    }
}

internal static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry is { } targetEntry &&
            targetEntry.Metadata.IsOwned() &&
            (targetEntry.State == EntityState.Added || targetEntry.State == EntityState.Modified));
}
