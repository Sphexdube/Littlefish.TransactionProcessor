namespace Transaction.Domain.Entities;

public sealed class RuleWorkflow
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public ICollection<BusinessRule> Rules { get; private set; } = new List<BusinessRule>();
}
