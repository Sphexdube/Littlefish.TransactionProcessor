namespace Transaction.Domain.Entities.Base;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; protected set; }
}
