using Transaction.Domain.Entities;

namespace Transaction.Tests.Unit.UnitTests.Domain.Entities;

public sealed class MerchantDailySummaryTests
{
    [Test]
    public void Create_ValidData_SetsAllProperties()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        const string merchantId = "MERCHANT-001";
        DateOnly date = DateOnly.FromDateTime(DateTime.UtcNow);
        const decimal initialAmount = 5000m;

        // Act
        MerchantDailySummary summary = MerchantDailySummary.Create(tenantId, merchantId, date, initialAmount);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(summary.TenantId, Is.EqualTo(tenantId));
            Assert.That(summary.MerchantId, Is.EqualTo(merchantId));
            Assert.That(summary.Date, Is.EqualTo(date));
            Assert.That(summary.TotalAmount, Is.EqualTo(initialAmount));
            Assert.That(summary.TransactionCount, Is.EqualTo(1));
            Assert.That(summary.LastCalculatedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }
    }

    [Test]
    public void AddAmount_PositiveAmount_IncreasesTotalAndCount()
    {
        // Arrange
        MerchantDailySummary summary = MerchantDailySummary.Create(
            Guid.NewGuid(), "MERCHANT-001", DateOnly.FromDateTime(DateTime.UtcNow), 1000m);

        // Act
        summary.AddAmount(2500m);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary.TotalAmount, Is.EqualTo(3500m));
            Assert.That(summary.TransactionCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void AddAmount_MultipleAdditions_AccumulatesCorrectly()
    {
        // Arrange
        MerchantDailySummary summary = MerchantDailySummary.Create(
            Guid.NewGuid(), "MERCHANT-001", DateOnly.FromDateTime(DateTime.UtcNow), 500m);

        // Act
        summary.AddAmount(300m);
        summary.AddAmount(200m);
        summary.AddAmount(100m);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary.TotalAmount, Is.EqualTo(1100m));
            Assert.That(summary.TransactionCount, Is.EqualTo(4));
        }
    }

    [Test]
    public void AddAmount_UpdatesLastCalculatedAt()
    {
        // Arrange
        MerchantDailySummary summary = MerchantDailySummary.Create(
            Guid.NewGuid(), "MERCHANT-001", DateOnly.FromDateTime(DateTime.UtcNow), 100m);

        DateTimeOffset beforeAdd = DateTimeOffset.UtcNow;

        // Act
        summary.AddAmount(50m);

        // Assert
        Assert.That(summary.LastCalculatedAt, Is.GreaterThanOrEqualTo(beforeAdd));
    }

    [Test]
    public void Create_TwoSummaries_HaveDifferentIds()
    {
        // Act
        MerchantDailySummary first = MerchantDailySummary.Create(
            Guid.NewGuid(), "MERCHANT-001", DateOnly.FromDateTime(DateTime.UtcNow), 100m);

        MerchantDailySummary second = MerchantDailySummary.Create(
            Guid.NewGuid(), "MERCHANT-002", DateOnly.FromDateTime(DateTime.UtcNow), 200m);

        // Assert
        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
