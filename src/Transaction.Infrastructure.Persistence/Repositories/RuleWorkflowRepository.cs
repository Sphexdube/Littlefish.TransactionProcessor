using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public sealed class RuleWorkflowRepository(TransactionDbContext context) : IRuleWorkflowRepository
{
    public async Task<RuleWorkflow?> GetByNameAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        return await context.RuleWorkflows
            .Include(w => w.Rules.Where(r => r.IsActive).OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(w => w.Name == workflowName && w.IsActive, cancellationToken);
    }
}
