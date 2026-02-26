namespace Transaction.Application.Models.Response.V1;

public record TransactionValidationError(string TransactionId, string ErrorMessage);
