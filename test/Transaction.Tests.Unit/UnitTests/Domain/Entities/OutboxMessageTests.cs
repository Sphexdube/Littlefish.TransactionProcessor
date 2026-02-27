using Transaction.Domain.Entities;

namespace Transaction.Tests.Unit.UnitTests.Domain.Entities;

public sealed class OutboxMessageTests
{
    [Test]
    public void Create_ValidData_SetsAllProperties()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        const string transactionId = "TXN-OUTBOX-001";
        const string payload = "{\"transactionId\":\"TXN-OUTBOX-001\"}";

        // Act
        OutboxMessage message = OutboxMessage.Create(id, tenantId, transactionId, payload);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Id, Is.EqualTo(id));
            Assert.That(message.TenantId, Is.EqualTo(tenantId));
            Assert.That(message.TransactionId, Is.EqualTo(transactionId));
            Assert.That(message.Payload, Is.EqualTo(payload));
            Assert.That(message.Published, Is.False);
            Assert.That(message.PublishedAt, Is.Null);
            Assert.That(message.CreatedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }
    }

    [Test]
    public void MarkAsPublished_SetsPublishedTrueAndPublishedAt()
    {
        // Arrange
        OutboxMessage message = OutboxMessage.Create(
            Guid.NewGuid(), Guid.NewGuid(), "TXN-001", "{}");

        Assert.That(message.Published, Is.False);

        DateTimeOffset beforePublish = DateTimeOffset.UtcNow;

        // Act
        message.MarkAsPublished();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Published, Is.True);
            Assert.That(message.PublishedAt, Is.Not.Null);
            Assert.That(message.PublishedAt!.Value, Is.GreaterThanOrEqualTo(beforePublish));
            Assert.That(message.UpdatedAt, Is.Not.Null);
        }
    }

    [Test]
    public void MarkAsPublished_CalledTwice_RemainsPublished()
    {
        // Arrange
        OutboxMessage message = OutboxMessage.Create(
            Guid.NewGuid(), Guid.NewGuid(), "TXN-001", "{}");

        // Act
        message.MarkAsPublished();
        message.MarkAsPublished();

        // Assert
        Assert.That(message.Published, Is.True);
    }

    [Test]
    public void Create_NewMessage_IsNotPublished()
    {
        // Act
        OutboxMessage message = OutboxMessage.Create(
            Guid.NewGuid(), Guid.NewGuid(), "TXN-NEW", "{\"data\":\"test\"}");

        // Assert
        Assert.That(message.Published, Is.False);
        Assert.That(message.PublishedAt, Is.Null);
    }
}
