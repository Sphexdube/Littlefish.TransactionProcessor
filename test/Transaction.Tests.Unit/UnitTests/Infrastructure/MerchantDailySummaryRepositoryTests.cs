using Microsoft.Extensions.DependencyInjection;
using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Infrastructure;

public sealed class MerchantDailySummaryRepositoryTests
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
    public async Task GetByMerchantAndDateAsync_ExistingSummary_ReturnsSummary()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IMerchantDailySummaryRepository repository = _serviceProvider!.GetRequiredService<IMerchantDailySummaryRepository>();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        MerchantDailySummary summary = MerchantDailySummary.Create(
            DefaultTestIds.TenantId,
            DefaultTestIds.MerchantId,
            today,
            5000m);

        await context.MerchantDailySummaries.AddAsync(summary);
        await context.SaveChangesAsync();

        // Act
        MerchantDailySummary? result = await repository.GetByMerchantAndDateAsync(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, today);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.MerchantId, Is.EqualTo(DefaultTestIds.MerchantId));
            Assert.That(result!.TenantId, Is.EqualTo(DefaultTestIds.TenantId));
            Assert.That(result!.Date, Is.EqualTo(today));
            Assert.That(result!.TotalAmount, Is.EqualTo(5000m));
            Assert.That(result!.TransactionCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetByMerchantAndDateAsync_WrongMerchantId_ReturnsNull()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IMerchantDailySummaryRepository repository = _serviceProvider!.GetRequiredService<IMerchantDailySummaryRepository>();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        MerchantDailySummary summary = MerchantDailySummary.Create(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, today, 1000m);

        await context.MerchantDailySummaries.AddAsync(summary);
        await context.SaveChangesAsync();

        // Act
        MerchantDailySummary? result = await repository.GetByMerchantAndDateAsync(
            DefaultTestIds.TenantId, "MERCHANT-WRONG", today);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByMerchantAndDateAsync_WrongDate_ReturnsNull()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IMerchantDailySummaryRepository repository = _serviceProvider!.GetRequiredService<IMerchantDailySummaryRepository>();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly yesterday = today.AddDays(-1);

        MerchantDailySummary summary = MerchantDailySummary.Create(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, today, 1000m);

        await context.MerchantDailySummaries.AddAsync(summary);
        await context.SaveChangesAsync();

        // Act — query for yesterday when only today's summary exists
        MerchantDailySummary? result = await repository.GetByMerchantAndDateAsync(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, yesterday);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByMerchantAndDateAsync_WrongTenantId_ReturnsNull()
    {
        // Arrange
        TransactionDbContext context = _serviceProvider!.GetRequiredService<TransactionDbContext>();
        IMerchantDailySummaryRepository repository = _serviceProvider!.GetRequiredService<IMerchantDailySummaryRepository>();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        MerchantDailySummary summary = MerchantDailySummary.Create(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, today, 1000m);

        await context.MerchantDailySummaries.AddAsync(summary);
        await context.SaveChangesAsync();

        // Act — query with a different TenantId
        MerchantDailySummary? result = await repository.GetByMerchantAndDateAsync(
            Guid.NewGuid(), DefaultTestIds.MerchantId, today);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByMerchantAndDateAsync_NoSummaries_ReturnsNull()
    {
        // Arrange
        IMerchantDailySummaryRepository repository = _serviceProvider!.GetRequiredService<IMerchantDailySummaryRepository>();

        // Act
        MerchantDailySummary? result = await repository.GetByMerchantAndDateAsync(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddAsync_NewSummary_CanBeRetrieved()
    {
        // Arrange
        IMerchantDailySummaryRepository repository = _serviceProvider!.GetRequiredService<IMerchantDailySummaryRepository>();
        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        MerchantDailySummary newSummary = MerchantDailySummary.Create(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, today, 2500m);

        // Act
        await repository.AddAsync(newSummary);
        await unitOfWork.SaveChangesAsync();

        MerchantDailySummary? retrieved = await repository.GetByMerchantAndDateAsync(
            DefaultTestIds.TenantId, DefaultTestIds.MerchantId, today);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.TotalAmount, Is.EqualTo(2500m));
            Assert.That(retrieved!.TransactionCount, Is.EqualTo(1));
        }
    }
}
