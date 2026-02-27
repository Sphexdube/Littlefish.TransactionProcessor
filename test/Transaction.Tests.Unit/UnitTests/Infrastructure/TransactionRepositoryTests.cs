using Microsoft.Extensions.DependencyInjection;
using Transaction.Application.Models.Request.V1;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Infrastructure;

public sealed class TransactionRepositoryTests
{
    private ServiceProvider? _serviceProvider;

    [SetUp]
    public async Task SetUp()
    {
        _serviceProvider = TestSetup.CreateServiceProvider();
        await DefaultDataSetup.AddDefaultTenant(_serviceProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task GetByTransactionIdAsync_ExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        Batch batch = Batch.Create(DefaultTestIds.TenantId, 1, DefaultTestIds.CorrelationId);
        await context.Batches.AddAsync(batch);

        TransactionRecord transaction = TransactionRecord.Create(
            DefaultTestIds.TenantId,
            batch.Id,
            "TXN-REPO-001",
            DefaultTestIds.MerchantId,
            1000m,
            "ZAR",
            TransactionType.Purchase,
            null,
            DateTimeOffset.UtcNow,
            null);

        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        // Act
        TransactionRecord? result = await unitOfWork.Transactions.GetByTransactionIdAsync(
            DefaultTestIds.TenantId, "TXN-REPO-001");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.TransactionId, Is.EqualTo("TXN-REPO-001"));
            Assert.That(result!.Amount, Is.EqualTo(1000m));
            Assert.That(result!.Currency, Is.EqualTo("ZAR"));
            Assert.That(result!.Type, Is.EqualTo(TransactionType.Purchase));
        }
    }

    [Test]
    public async Task GetByTransactionIdAsync_NonExistentTransaction_ReturnsNull()
    {
        // Arrange
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        // Act
        TransactionRecord? result = await unitOfWork.Transactions.GetByTransactionIdAsync(
            DefaultTestIds.TenantId, "TXN-DOES-NOT-EXIST");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ExistsByTransactionIdAsync_ExistingTransaction_ReturnsTrue()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        Batch batch = Batch.Create(DefaultTestIds.TenantId, 1, DefaultTestIds.CorrelationId);
        await context.Batches.AddAsync(batch);

        TransactionRecord transaction = TransactionRecord.Create(
            DefaultTestIds.TenantId,
            batch.Id,
            "TXN-EXISTS-001",
            DefaultTestIds.MerchantId,
            500m,
            "ZAR",
            TransactionType.Purchase,
            null,
            DateTimeOffset.UtcNow,
            null);

        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        // Act
        bool exists = await unitOfWork.Transactions.ExistsByTransactionIdAsync(
            DefaultTestIds.TenantId, "TXN-EXISTS-001");

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task ExistsByTransactionIdAsync_NonExistentTransaction_ReturnsFalse()
    {
        // Arrange
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        // Act
        bool exists = await unitOfWork.Transactions.ExistsByTransactionIdAsync(
            DefaultTestIds.TenantId, "TXN-GHOST-999");

        // Assert
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task FindAsync_FilterByBatchId_ReturnsOnlyMatchingTransactions()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        Batch batchA = Batch.Create(DefaultTestIds.TenantId, 2, "CORR-A");
        Batch batchB = Batch.Create(DefaultTestIds.TenantId, 1, "CORR-B");
        await context.Batches.AddRangeAsync([batchA, batchB]);

        TransactionRecord txnA1 = TransactionRecord.Create(DefaultTestIds.TenantId, batchA.Id, "TXN-A1", "M1", 100m, "ZAR", TransactionType.Purchase, null, DateTimeOffset.UtcNow, null);
        TransactionRecord txnA2 = TransactionRecord.Create(DefaultTestIds.TenantId, batchA.Id, "TXN-A2", "M1", 200m, "ZAR", TransactionType.Purchase, null, DateTimeOffset.UtcNow, null);
        TransactionRecord txnB1 = TransactionRecord.Create(DefaultTestIds.TenantId, batchB.Id, "TXN-B1", "M2", 300m, "ZAR", TransactionType.Purchase, null, DateTimeOffset.UtcNow, null);

        await context.Transactions.AddRangeAsync([txnA1, txnA2, txnB1]);
        await context.SaveChangesAsync();

        // Act
        IEnumerable<TransactionRecord> batchATransactions =
            await unitOfWork.Transactions.FindAsync(t => t.BatchId == batchA.Id);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(batchATransactions.Count(), Is.EqualTo(2));
            Assert.That(batchATransactions.All(t => t.BatchId == batchA.Id), Is.True);
        }
    }
}
