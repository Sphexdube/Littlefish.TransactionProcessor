using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Transaction.Application.Models.Request.V1;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Application.Validators.V1;

public sealed class IngestTransactionBatchRequestValidatorTests
{
    private ServiceProvider? _serviceProvider;
    private IValidator<IngestTransactionBatchRequest>? _validator;

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = TestSetup.CreateServiceProvider();
        _validator = _serviceProvider.GetRequiredService<IValidator<IngestTransactionBatchRequest>>();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task Validate_ValidBatchWith100Transactions_ShouldNotHaveErrors()
    {
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(100).Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Validate_EmptyTransactionsList_ShouldHaveError()
    {
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder()
            .WithTransactions([])
            .Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Transactions);
    }

    [Test]
    public async Task Validate_BatchWithFewerThan100Transactions_ShouldHaveError()
    {
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(50).Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Transactions)
            .WithErrorMessage("Batch must contain between 100 and 5000 transactions");
    }

    [Test]
    public async Task Validate_BatchWithMoreThan5000Transactions_ShouldHaveError()
    {
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(5001).Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Transactions)
            .WithErrorMessage("Batch must contain between 100 and 5000 transactions");
    }

    [Test]
    public async Task Validate_BatchWithExactly5000Transactions_ShouldNotHaveErrors()
    {
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(5000).Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Validate_TransactionWithInvalidType_ShouldHaveError()
    {
        IList<TransactionItemRequest> transactions =
        [
            new TransactionItemRequestBuilder("TXN-001").WithType("INVALID_TYPE").Build(),
            .. IngestTransactionBatchRequestBuilder.GenerateTransactions(99)
        ];

        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder()
            .WithTransactions(transactions)
            .Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveAnyValidationError();
    }

    [Test]
    public async Task Validate_RefundWithoutOriginalTransactionId_ShouldHaveError()
    {
        IList<TransactionItemRequest> transactions =
        [
            new TransactionItemRequestBuilder("TXN-REFUND-001")
                .WithType("REFUND")
                .WithOriginalTransactionId(null)
                .Build(),
            .. IngestTransactionBatchRequestBuilder.GenerateTransactions(99)
        ];

        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder()
            .WithTransactions(transactions)
            .Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveAnyValidationError();
    }

    [Test]
    public async Task Validate_RefundWithOriginalTransactionId_ShouldNotHaveError()
    {
        IList<TransactionItemRequest> transactions =
        [
            new TransactionItemRequestBuilder("TXN-REFUND-001")
                .WithType("REFUND")
                .WithOriginalTransactionId("TXN-ORIGINAL-001")
                .Build(),
            .. IngestTransactionBatchRequestBuilder.GenerateTransactions(99)
        ];

        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder()
            .WithTransactions(transactions)
            .Build();

        TestValidationResult<IngestTransactionBatchRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
