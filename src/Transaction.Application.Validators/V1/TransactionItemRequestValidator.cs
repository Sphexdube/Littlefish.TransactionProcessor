using FluentValidation;
using Transaction.Application.Models.Request.V1;

namespace Transaction.Application.Validators.V1;

public class TransactionItemRequestValidator : AbstractValidator<TransactionItemRequest>
{
    private static readonly string[] ValidTransactionTypes = ["PURCHASE", "REFUND", "REVERSAL"];

    public TransactionItemRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("Merchant ID is required");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be 3 characters");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Transaction type is required")
            .Must(t => ValidTransactionTypes.Contains(t.ToUpperInvariant()))
            .WithMessage("Transaction type must be PURCHASE, REFUND, or REVERSAL");

        RuleFor(x => x.OccurredAt)
            .NotEmpty()
            .WithMessage("OccurredAt timestamp is required");

        RuleFor(x => x.OriginalTransactionId)
            .NotEmpty()
            .When(x => x.Type.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
            .WithMessage("OriginalTransactionId is required for REFUND transactions");
    }
}
