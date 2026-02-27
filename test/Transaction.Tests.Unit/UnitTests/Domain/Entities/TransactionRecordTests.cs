using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Tests.Unit.UnitTests.Domain.Entities;

public sealed class TransactionRecordTests
{
    private static TransactionRecord CreateDefaultTransaction(
        TransactionType type = TransactionType.Purchase,
        string? originalTransactionId = null) =>
        TransactionRecord.Create(
            tenantId: Guid.NewGuid(),
            batchId: Guid.NewGuid(),
            transactionId: "TXN-ENTITY-001",
            merchantId: "MERCHANT-001",
            amount: 5000m,
            currency: "USD",
            type: type,
            originalTransactionId: originalTransactionId,
            occurredAt: DateTimeOffset.UtcNow,
            metadata: null);

    [Test]
    public void Create_ValidData_SetsAllProperties()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid batchId = Guid.NewGuid();
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;

        // Act
        TransactionRecord transaction = TransactionRecord.Create(
            tenantId: tenantId,
            batchId: batchId,
            transactionId: "TXN-001",
            merchantId: "MER-001",
            amount: 1500m,
            currency: "ZAR",
            type: TransactionType.Purchase,
            originalTransactionId: null,
            occurredAt: occurredAt,
            metadata: "{\"key\":\"value\"}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(transaction.TenantId, Is.EqualTo(tenantId));
            Assert.That(transaction.BatchId, Is.EqualTo(batchId));
            Assert.That(transaction.TransactionId, Is.EqualTo("TXN-001"));
            Assert.That(transaction.MerchantId, Is.EqualTo("MER-001"));
            Assert.That(transaction.Amount, Is.EqualTo(1500m));
            Assert.That(transaction.Currency, Is.EqualTo("ZAR"));
            Assert.That(transaction.Type, Is.EqualTo(TransactionType.Purchase));
            Assert.That(transaction.OriginalTransactionId, Is.Null);
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Received));
            Assert.That(transaction.OccurredAt, Is.EqualTo(occurredAt));
            Assert.That(transaction.Metadata, Is.EqualTo("{\"key\":\"value\"}"));
            Assert.That(transaction.RejectionReason, Is.Null);
            Assert.That(transaction.ProcessedAt, Is.Null);
        }
    }

    [Test]
    public void Create_RefundTransaction_SetsOriginalTransactionId()
    {
        // Act
        TransactionRecord transaction = CreateDefaultTransaction(
            type: TransactionType.Refund,
            originalTransactionId: "TXN-ORIGINAL-001");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction.Type, Is.EqualTo(TransactionType.Refund));
            Assert.That(transaction.OriginalTransactionId, Is.EqualTo("TXN-ORIGINAL-001"));
        }
    }

    [Test]
    public void BeginProcessing_ChangesStatusToProcessing()
    {
        // Arrange
        TransactionRecord transaction = CreateDefaultTransaction();
        Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Received));

        // Act
        transaction.BeginProcessing();

        // Assert
        Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Processing));
    }

    [Test]
    public void Complete_ChangesStatusToProcessedAndSetsProcessedAt()
    {
        // Arrange
        TransactionRecord transaction = CreateDefaultTransaction();

        // Act
        transaction.Complete();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Processed));
            Assert.That(transaction.ProcessedAt, Is.Not.Null);
            Assert.That(transaction.ProcessedAt!.Value, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }
    }

    [Test]
    public void Reject_ChangesStatusToRejectedAndSetsReason()
    {
        // Arrange
        TransactionRecord transaction = CreateDefaultTransaction();
        const string rejectionReason = "Daily limit exceeded";

        // Act
        transaction.Reject(rejectionReason);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Rejected));
            Assert.That(transaction.RejectionReason, Is.EqualTo(rejectionReason));
            Assert.That(transaction.ProcessedAt, Is.Not.Null);
        }
    }

    [Test]
    public void MarkForReview_ChangesStatusToReviewAndSetsReason()
    {
        // Arrange
        TransactionRecord transaction = CreateDefaultTransaction();
        const string reviewReason = "Transaction amount exceeds high-value threshold";

        // Act
        transaction.MarkForReview(reviewReason);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Review));
            Assert.That(transaction.RejectionReason, Is.EqualTo(reviewReason));
            Assert.That(transaction.ProcessedAt, Is.Not.Null);
        }
    }

    [Test]
    public void MarkForReview_NullReason_SetsNullRejectionReason()
    {
        // Arrange
        TransactionRecord transaction = CreateDefaultTransaction();

        // Act
        transaction.MarkForReview(null);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Review));
            Assert.That(transaction.RejectionReason, Is.Null);
        }
    }

    [Test]
    public void Create_TwoTransactions_HaveDifferentIds()
    {
        // Act
        TransactionRecord first = CreateDefaultTransaction();
        TransactionRecord second = CreateDefaultTransaction();

        // Assert
        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
