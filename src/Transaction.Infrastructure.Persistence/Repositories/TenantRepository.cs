using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public class TenantRepository : Repository<Tenant, Guid>, ITenantRepository
{
    public TenantRepository(TransactionDbContext context)
        : base(context)
    {
    }

    public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }
}
