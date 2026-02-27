using Microsoft.Extensions.DependencyInjection;
using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Infrastructure;

public sealed class OutboxMessageRepositoryTests
{
    private ServiceProvider? _serviceProvider;

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = TestSetup.CreateServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task GetUnpublishedAsync_WithUnpublishedMessages_ReturnsOnlyUnpublished()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        OutboxMessage unpublished1 = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-001", "{}");
        OutboxMessage unpublished2 = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-002", "{}");
        OutboxMessage published = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-003", "{}");
        published.MarkAsPublished();

        await context.OutboxMessages.AddRangeAsync([unpublished1, unpublished2, published]);
        await context.SaveChangesAsync();

        // Act
        IReadOnlyList<OutboxMessage> result = await unitOfWork.OutboxMessages.GetUnpublishedAsync(batchSize: 10);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(m => !m.Published), Is.True);
        }
    }

    [Test]
    public async Task GetUnpublishedAsync_RespectsBatchSize_ReturnsOnlyUpToLimit()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        OutboxMessage[] messages = Enumerable.Range(1, 5)
            .Select(i => OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, $"TXN-BATCH-{i:000}", "{}"))
            .ToArray();

        await context.OutboxMessages.AddRangeAsync(messages);
        await context.SaveChangesAsync();

        // Act
        IReadOnlyList<OutboxMessage> result = await unitOfWork.OutboxMessages.GetUnpublishedAsync(batchSize: 3);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetUnpublishedAsync_OrdersByCreatedAt_ReturnsOldestFirst()
    {
        // Arrange — create messages with staggered CreatedAt by saving between each
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        OutboxMessage first = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-OLDEST", "{}");
        await context.OutboxMessages.AddAsync(first);
        await context.SaveChangesAsync();

        await Task.Delay(5); // ensure different CreatedAt timestamps

        OutboxMessage second = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-NEWEST", "{}");
        await context.OutboxMessages.AddAsync(second);
        await context.SaveChangesAsync();

        // Act
        IReadOnlyList<OutboxMessage> result = await unitOfWork.OutboxMessages.GetUnpublishedAsync(batchSize: 10);

        // Assert — oldest should come first
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].TransactionId, Is.EqualTo("TXN-OLDEST"));
            Assert.That(result[1].TransactionId, Is.EqualTo("TXN-NEWEST"));
        }
    }

    [Test]
    public async Task GetUnpublishedAsync_AllPublished_ReturnsEmpty()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        OutboxMessage published = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-PUB", "{}");
        published.MarkAsPublished();
        await context.OutboxMessages.AddAsync(published);
        await context.SaveChangesAsync();

        // Act
        IReadOnlyList<OutboxMessage> result = await unitOfWork.OutboxMessages.GetUnpublishedAsync(batchSize: 10);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetUnpublishedAsync_EmptyDatabase_ReturnsEmpty()
    {
        // Arrange
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        // Act
        IReadOnlyList<OutboxMessage> result = await unitOfWork.OutboxMessages.GetUnpublishedAsync(batchSize: 100);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task MarkAsPublished_UpdatesPublishedFlagInDatabase()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        OutboxMessage message = OutboxMessage.Create(Guid.NewGuid(), DefaultTestIds.TenantId, "TXN-MARK-001", "{}");
        await context.OutboxMessages.AddAsync(message);
        await context.SaveChangesAsync();

        // Act
        message.MarkAsPublished();
        await context.SaveChangesAsync();

        // Assert — re-fetch and verify
        IReadOnlyList<OutboxMessage> unpublished = await unitOfWork.OutboxMessages.GetUnpublishedAsync(batchSize: 10);
        Assert.That(unpublished.Any(m => m.TransactionId == "TXN-MARK-001"), Is.False);
    }
}
