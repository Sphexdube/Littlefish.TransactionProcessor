using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface IRuleWorkflowRepository
{
    Task<RuleWorkflow?> GetByNameAsync(string workflowName, CancellationToken cancellationToken = default);
}
