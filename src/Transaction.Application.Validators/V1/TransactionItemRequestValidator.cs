using FluentValidation;
using Transaction.Application.Constants;
using Transaction.Application.Models.Request.V1;

namespace Transaction.Application.Validators.V1;

public class TransactionItemRequestValidator : AbstractValidator<TransactionItemRequest>
{
    private static readonly string[] ValidTransactionTypes = ["PURCHASE", "REFUND", "REVERSAL"];

    public TransactionItemRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage(ValidationMessages.TransactionIdRequired);

        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage(ValidationMessages.MerchantIdRequired);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage(ValidationMessages.CurrencyRequired)
            .Length(3)
            .WithMessage(ValidationMessages.CurrencyInvalidLength);

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage(ValidationMessages.TypeRequired)
            .Must(t => ValidTransactionTypes.Contains(t.ToUpperInvariant()))
            .WithMessage(ValidationMessages.TypeInvalid);

        RuleFor(x => x.OccurredAt)
            .NotEmpty()
            .WithMessage(ValidationMessages.OccurredAtRequired);

        RuleFor(x => x.OriginalTransactionId)
            .NotEmpty()
            .When(x => x.Type.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
            .WithMessage(ValidationMessages.OriginalTransactionIdRequiredForRefund);
    }
}
