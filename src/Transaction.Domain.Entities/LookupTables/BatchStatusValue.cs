namespace Transaction.Domain.Entities.LookupTables;

public sealed class BatchStatusValue
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }
}
