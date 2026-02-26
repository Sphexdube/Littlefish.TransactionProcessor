using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface ITenantRepository : IRepository<Tenant, Guid>
{
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
