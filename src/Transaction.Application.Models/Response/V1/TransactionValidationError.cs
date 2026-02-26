namespace Transaction.Application.Models.Response.V1;

public sealed record TransactionValidationError
{
    public required string TransactionId { get; init; }

    public required string ErrorMessage { get; init; }
}
