using System.Reflection;
using NSubstitute;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Rules;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Models;

namespace Transaction.Tests.Unit.UnitTests.Domain.Rules;

public sealed class RulesEngineAdapterTests
{
    private ITransactionRepository _transactionRepository = null!;
    private IRuleWorkflowRepository _ruleWorkflowRepository = null!;
    private RulesEngineAdapter _adapter = null!;
    private Tenant _defaultTenant = null!;

    [SetUp]
    public void SetUp()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _ruleWorkflowRepository = Substitute.For<IRuleWorkflowRepository>();
        _adapter = new RulesEngineAdapter(_transactionRepository, _ruleWorkflowRepository);
        _defaultTenant = TenantBuilder.BuildDefault();
    }

    [Test]
    public void EvaluateAllAsync_WorkflowNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RuleWorkflow?)null);

        TransactionRecord transaction = CreatePurchaseTransaction(amount: 1000m);
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.EvaluateAllAsync(context));
    }

    [Test]
    public async Task EvaluateAllAsync_ValidPurchaseBelowThreshold_ReturnsOnlySuccessResults()
    {
        // Arrange — amount 1000 is below HighValueThreshold of 10000; projected total below DailyMerchantLimit
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        TransactionRecord transaction = CreatePurchaseTransaction(amount: 1000m);
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act
        IEnumerable<RuleResult> results = await _adapter.EvaluateAllAsync(context);

        // Assert — all non-HighValueReview rules pass; HighValueReview fails (amount <= threshold) so no NeedsReview
        List<RuleResult> resultList = results.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resultList, Is.Not.Empty);
            Assert.That(resultList.All(r => r.IsValid), Is.True);
            Assert.That(resultList.Any(r => r.RequiresReview), Is.False);
        }
    }

    [Test]
    public async Task EvaluateAllAsync_PurchaseAboveHighValueThreshold_ReturnsNeedsReview()
    {
        // Arrange — amount 15000 exceeds HighValueThreshold of 10000
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        TransactionRecord transaction = CreatePurchaseTransaction(amount: 15_000m);
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act
        IEnumerable<RuleResult> results = await _adapter.EvaluateAllAsync(context);

        // Assert — HighValueReview succeeds → NeedsReview added
        List<RuleResult> resultList = results.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resultList, Is.Not.Empty);
            Assert.That(resultList.Any(r => r.RequiresReview), Is.True);
            RuleResult? reviewResult = resultList.FirstOrDefault(r => r.RequiresReview);
            Assert.That(reviewResult, Is.Not.Null);
            Assert.That(reviewResult!.IsValid, Is.True);
            Assert.That(reviewResult.ErrorMessage, Is.EqualTo(RuleMessages.HighValueThresholdReview));
        }
    }

    [Test]
    public async Task EvaluateAllAsync_PurchaseExceedsDailyLimit_ReturnsFailure()
    {
        // Arrange — projected total 110000 exceeds DailyMerchantLimit of 100000
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        TransactionRecord transaction = CreatePurchaseTransaction(amount: 10_000m);
        // CurrentDailyMerchantTotal = 95000; projected = 105000 > 100000 (limit)
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 95_000m);

        // Act
        IEnumerable<RuleResult> results = await _adapter.EvaluateAllAsync(context);

        // Assert
        List<RuleResult> resultList = results.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resultList, Is.Not.Empty);
            Assert.That(resultList.Any(r => !r.IsValid), Is.True);
            RuleResult? failureResult = resultList.FirstOrDefault(r => !r.IsValid);
            Assert.That(failureResult, Is.Not.Null);
            Assert.That(failureResult!.RequiresReview, Is.False);
        }
    }

    [Test]
    public async Task EvaluateAllAsync_RefundWithExistingOriginalPurchase_ReturnsSuccessResults()
    {
        // Arrange
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        _transactionRepository
            .ExistsByTransactionIdAsync(
                DefaultTestIds.TenantId,
                "TXN-ORIGINAL-001",
                Arg.Any<CancellationToken>())
            .Returns(true);

        TransactionRecord transaction = CreateRefundTransaction(originalTransactionId: "TXN-ORIGINAL-001");
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act
        IEnumerable<RuleResult> results = await _adapter.EvaluateAllAsync(context);

        // Assert — RefundRequiresOriginalPurchase should pass (OriginalPurchaseExists = true)
        List<RuleResult> resultList = results.ToList();
        Assert.That(resultList.Any(r => !r.IsValid), Is.False);
    }

    [Test]
    public async Task EvaluateAllAsync_RefundWithoutOriginalPurchase_ReturnsFailure()
    {
        // Arrange
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        _transactionRepository
            .ExistsByTransactionIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        TransactionRecord transaction = CreateRefundTransaction(originalTransactionId: "TXN-NONEXISTENT-999");
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act
        IEnumerable<RuleResult> results = await _adapter.EvaluateAllAsync(context);

        // Assert — RefundRequiresOriginalPurchase fails
        List<RuleResult> resultList = results.ToList();
        Assert.That(resultList.Any(r => !r.IsValid), Is.True);
    }

    [Test]
    public async Task EvaluateAllAsync_RefundType_CallsExistsByTransactionIdAsync()
    {
        // Arrange
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        _transactionRepository
            .ExistsByTransactionIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        TransactionRecord transaction = CreateRefundTransaction(originalTransactionId: "TXN-ORIGINAL-001");
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act
        await _adapter.EvaluateAllAsync(context);

        // Assert — verify repository was called to check original purchase
        await _transactionRepository.Received(1).ExistsByTransactionIdAsync(
            DefaultTestIds.TenantId,
            "TXN-ORIGINAL-001",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAllAsync_PurchaseType_DoesNotCallExistsByTransactionIdAsync()
    {
        // Arrange
        _ruleWorkflowRepository
            .GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildStandardWorkflow());

        TransactionRecord transaction = CreatePurchaseTransaction(amount: 500m);
        RuleContext context = new RuleContext(transaction, _defaultTenant, CurrentDailyMerchantTotal: 0m);

        // Act
        await _adapter.EvaluateAllAsync(context);

        // Assert — original purchase lookup should NOT be called for Purchase transactions
        await _transactionRepository.DidNotReceive().ExistsByTransactionIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static TransactionRecord CreatePurchaseTransaction(decimal amount) =>
        TransactionRecord.Create(
            tenantId: DefaultTestIds.TenantId,
            batchId: Guid.NewGuid(),
            transactionId: "TXN-RULES-PURCHASE",
            merchantId: "MERCHANT-001",
            amount: amount,
            currency: "USD",
            type: TransactionType.Purchase,
            originalTransactionId: null,
            occurredAt: DateTimeOffset.UtcNow,
            metadata: null);

    private static TransactionRecord CreateRefundTransaction(string originalTransactionId) =>
        TransactionRecord.Create(
            tenantId: DefaultTestIds.TenantId,
            batchId: Guid.NewGuid(),
            transactionId: "TXN-RULES-REFUND",
            merchantId: "MERCHANT-001",
            amount: 500m,
            currency: "USD",
            type: TransactionType.Refund,
            originalTransactionId: originalTransactionId,
            occurredAt: DateTimeOffset.UtcNow,
            metadata: null);

    /// <summary>
    /// Builds a <see cref="RuleWorkflow"/> equivalent to the production DB seed data
    /// (src/dbTransactionProcessor/up/LOCAL/0006_INSERT_seed_Rules.ENV.LOCAL.sql).
    /// Uses reflection to instantiate entities with private constructors.
    /// </summary>
    private static RuleWorkflow BuildStandardWorkflow()
    {
        BusinessRule negativePurchaseAmount = CreateBusinessRule(
            ruleName: RuleMessages.NegativePurchaseAmountRule,
            expression: "input1.TransactionType != \"Purchase\" || input1.Amount > 0",
            errorMessage: RuleMessages.NegativePurchaseAmountError);

        BusinessRule refundRequiresOriginalPurchase = CreateBusinessRule(
            ruleName: RuleMessages.RefundRequiresOriginalPurchaseRule,
            expression: "input1.TransactionType != \"Refund\" || input1.OriginalPurchaseExists == true",
            errorMessage: RuleMessages.RefundRequiresOriginalPurchaseError);

        BusinessRule dailyMerchantLimit = CreateBusinessRule(
            ruleName: RuleMessages.DailyMerchantLimitRule,
            expression: "input1.TransactionType != \"Purchase\" || input1.ProjectedDailyTotal <= input1.DailyMerchantLimit",
            errorMessage: RuleMessages.DailyMerchantLimitError);

        BusinessRule highValueReview = CreateBusinessRule(
            ruleName: RuleMessages.HighValueReviewRuleName,
            expression: "input1.Amount > input1.HighValueThreshold",
            successEvent: RuleMessages.ReviewSuccessEvent);

        List<BusinessRule> rules =
        [
            negativePurchaseAmount,
            refundRequiresOriginalPurchase,
            dailyMerchantLimit,
            highValueReview
        ];

        return CreateRuleWorkflow(RuleMessages.TransactionRulesWorkflow, rules);
    }

    private static BusinessRule CreateBusinessRule(
        string ruleName,
        string expression,
        string? errorMessage = null,
        string? successEvent = null)
    {
        BusinessRule rule = (BusinessRule)Activator.CreateInstance(typeof(BusinessRule), nonPublic: true)!;
        SetProperty(rule, nameof(BusinessRule.RuleName), ruleName);
        SetProperty(rule, nameof(BusinessRule.RuleExpressionType), RuleMessages.LambdaExpressionType);
        SetProperty(rule, nameof(BusinessRule.Expression), expression);
        SetProperty(rule, nameof(BusinessRule.ErrorMessage), errorMessage);
        SetProperty(rule, nameof(BusinessRule.SuccessEvent), successEvent);
        SetProperty(rule, nameof(BusinessRule.IsActive), true);
        return rule;
    }

    private static RuleWorkflow CreateRuleWorkflow(string name, ICollection<BusinessRule> rules)
    {
        RuleWorkflow workflow = (RuleWorkflow)Activator.CreateInstance(typeof(RuleWorkflow), nonPublic: true)!;
        SetProperty(workflow, nameof(RuleWorkflow.Name), name);
        SetProperty(workflow, nameof(RuleWorkflow.IsActive), true);
        SetProperty(workflow, nameof(RuleWorkflow.Rules), (ICollection<BusinessRule>)rules);
        return workflow;
    }

    private static void SetProperty<T>(T obj, string propertyName, object? value)
    {
        PropertyInfo property = typeof(T).GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance)!;
        property.SetValue(obj, value);
    }
}
