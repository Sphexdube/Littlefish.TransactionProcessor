using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Tests.Unit.UnitTests.Domain.Entities;

public sealed class BatchTests
{
    [Test]
    public void Create_ValidData_SetsAllProperties()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        const int totalCount = 150;
        const string correlationId = "CORR-BATCH-001";

        // Act
        Batch batch = Batch.Create(tenantId, totalCount, correlationId);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(batch.TenantId, Is.EqualTo(tenantId));
            Assert.That(batch.TotalCount, Is.EqualTo(totalCount));
            Assert.That(batch.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(batch.Status, Is.EqualTo(BatchStatus.Received));
            Assert.That(batch.AcceptedCount, Is.EqualTo(0));
            Assert.That(batch.RejectedCount, Is.EqualTo(0));
            Assert.That(batch.QueuedCount, Is.EqualTo(0));
            Assert.That(batch.CompletedAt, Is.Null);
        }
    }

    [Test]
    public void Create_TwoBatches_HaveDifferentIds()
    {
        // Act
        Batch first = Batch.Create(Guid.NewGuid(), 100, "CORR-001");
        Batch second = Batch.Create(Guid.NewGuid(), 100, "CORR-002");

        // Assert
        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void UpdateCounts_ValidCounts_UpdatesCountsAndChangesStatusToProcessing()
    {
        // Arrange
        Batch batch = Batch.Create(Guid.NewGuid(), 100, "CORR-001");

        // Act
        batch.UpdateCounts(accepted: 95, rejected: 5, queued: 95);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.AcceptedCount, Is.EqualTo(95));
            Assert.That(batch.RejectedCount, Is.EqualTo(5));
            Assert.That(batch.QueuedCount, Is.EqualTo(95));
            Assert.That(batch.Status, Is.EqualTo(BatchStatus.Processing));
        }
    }

    [Test]
    public void UpdateCounts_AllAccepted_SetsZeroRejected()
    {
        // Arrange
        Batch batch = Batch.Create(Guid.NewGuid(), 100, "CORR-001");

        // Act
        batch.UpdateCounts(accepted: 100, rejected: 0, queued: 100);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.AcceptedCount, Is.EqualTo(100));
            Assert.That(batch.RejectedCount, Is.EqualTo(0));
            Assert.That(batch.QueuedCount, Is.EqualTo(100));
        }
    }

    [Test]
    public void UpdateCounts_AllRejected_SetsZeroAccepted()
    {
        // Arrange
        Batch batch = Batch.Create(Guid.NewGuid(), 100, "CORR-001");

        // Act
        batch.UpdateCounts(accepted: 0, rejected: 100, queued: 0);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.AcceptedCount, Is.EqualTo(0));
            Assert.That(batch.RejectedCount, Is.EqualTo(100));
            Assert.That(batch.QueuedCount, Is.EqualTo(0));
        }
    }
}
