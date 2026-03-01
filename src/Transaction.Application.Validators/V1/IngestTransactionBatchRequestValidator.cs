using FluentValidation;
using Transaction.Application.Constants;
using Transaction.Application.Models.Request.V1;

namespace Transaction.Application.Validators.V1;

public class IngestTransactionBatchRequestValidator : AbstractValidator<IngestTransactionBatchRequest>
{
    public IngestTransactionBatchRequestValidator()
    {
        RuleFor(x => x.Transactions)
            .NotEmpty()
            .Must(t => t.Count >= 100 && t.Count <= 5000)
            .WithMessage(ValidationMessages.BatchSizeInvalid);

        RuleForEach(x => x.Transactions)
            .SetValidator(new TransactionItemRequestValidator());
    }
}
