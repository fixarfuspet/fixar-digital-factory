using Fixar.Domain.Common;

namespace Fixar.Application.Common.Interfaces;

/// <summary>
/// Coordinates one or more repositories against a single persistence
/// transaction. Call <see cref="SaveChangesAsync"/> once per business
/// operation after all repository mutations have been staged.
/// </summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
