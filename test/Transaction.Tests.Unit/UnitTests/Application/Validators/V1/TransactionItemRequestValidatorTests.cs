using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Transaction.Application.Models.Request.V1;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Application.Validators.V1;

public sealed class TransactionItemRequestValidatorTests
{
    private ServiceProvider? _serviceProvider;
    private IValidator<TransactionItemRequest>? _validator;

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = TestSetup.CreateServiceProvider();
        _validator = _serviceProvider.GetRequiredService<IValidator<TransactionItemRequest>>();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task Validate_ValidPurchaseTransaction_ShouldNotHaveErrors()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder().Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Validate_EmptyTransactionId_ShouldHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithTransactionId(string.Empty)
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorMessage("Transaction ID is required");
    }

    [Test]
    public async Task Validate_EmptyMerchantId_ShouldHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithMerchantId(string.Empty)
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.MerchantId)
            .WithErrorMessage("Merchant ID is required");
    }

    [Test]
    public async Task Validate_EmptyCurrency_ShouldHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithCurrency(string.Empty)
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required");
    }

    [Test]
    public async Task Validate_CurrencyNotThreeCharacters_ShouldHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithCurrency("ZA")
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be 3 characters");
    }

    [TestCase("PURCHASE", TestName = "Type_Purchase")]
    [TestCase("REFUND", TestName = "Type_Refund")]
    [TestCase("REVERSAL", TestName = "Type_Reversal")]
    public async Task Validate_ValidTransactionType_ShouldNotHaveError(string validType)
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithType(validType)
            .WithOriginalTransactionId(validType == "REFUND" ? "TXN-ORIG" : null)
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Test]
    public async Task Validate_InvalidTransactionType_ShouldHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithType("PAYMENT")
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Transaction type must be PURCHASE, REFUND, or REVERSAL");
    }

    [Test]
    public async Task Validate_RefundWithoutOriginalTransactionId_ShouldHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithType("REFUND")
            .WithOriginalTransactionId(null)
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.OriginalTransactionId)
            .WithErrorMessage("OriginalTransactionId is required for REFUND transactions");
    }

    [Test]
    public async Task Validate_RefundWithOriginalTransactionId_ShouldNotHaveError()
    {
        TransactionItemRequest request = new TransactionItemRequestBuilder()
            .WithType("REFUND")
            .WithOriginalTransactionId("TXN-ORIGINAL-001")
            .Build();

        TestValidationResult<TransactionItemRequest> result =
            await _validator!.TestValidateAsync(request);

        result.ShouldNotHaveValidationErrorFor(x => x.OriginalTransactionId);
    }
}
