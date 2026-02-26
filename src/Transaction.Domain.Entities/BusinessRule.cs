namespace Transaction.Domain.Entities;

public sealed class BusinessRule
{
    public int Id { get; private set; }

    public int WorkflowId { get; private set; }

    public string RuleName { get; private set; } = string.Empty;

    public string RuleExpressionType { get; private set; } = string.Empty;

    public string Expression { get; private set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public string? SuccessEvent { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }
}
