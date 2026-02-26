namespace Transaction.Domain.Entities.Base;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
