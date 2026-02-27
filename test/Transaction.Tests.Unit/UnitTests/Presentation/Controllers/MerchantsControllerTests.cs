using System.Net;
using Transaction.Application.Models.Response.V1;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;
using Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers;

public sealed class MerchantsControllerTests : BaseControllerTests
{
    [SetUp]
    public async Task SeedData()
    {
        await DefaultDataSetup.AddDefaultTenant(ServiceProvider!);
    }

    [Test]
    public async Task GetDailySummaryAsync_ExistingSummary_Returns200OK()
    {
        // Arrange
        DateOnly summaryDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        await DefaultDataSetup.AddMerchantDailySummary(
            ServiceProvider!,
            DefaultTestIds.MerchantId,
            summaryDate,
            totalAmount: 75_000m,
            transactionCount: 15);

        // Act
        ApiResponse<DailySummaryResponse> response = await MerchantsClient!.GetDailySummary(
            DefaultTestIds.TenantId,
            DefaultTestIds.MerchantId,
            summaryDate);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(response.IsError, Is.False);
            Assert.That(response.Result, Is.Not.Null);
            Assert.That(response.Result!.MerchantId, Is.EqualTo(DefaultTestIds.MerchantId));
            Assert.That(response.Result!.Date, Is.EqualTo(summaryDate));
            Assert.That(response.Result!.TotalAmount, Is.GreaterThan(0));
            Assert.That(response.Result!.TransactionCount, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task GetDailySummaryAsync_NonExistentMerchant_Returns404NotFound()
    {
        // Act
        ApiResponse<DailySummaryResponse> response = await MerchantsClient!.GetDailySummary(
            DefaultTestIds.TenantId,
            "MERCHANT-DOES-NOT-EXIST",
            DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetDailySummaryAsync_WrongDate_Returns404NotFound()
    {
        // Arrange — add a summary for today but query for yesterday
        await DefaultDataSetup.AddMerchantDailySummary(
            ServiceProvider!,
            DefaultTestIds.MerchantId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            totalAmount: 5000m,
            transactionCount: 5);

        // Act
        ApiResponse<DailySummaryResponse> response = await MerchantsClient!.GetDailySummary(
            DefaultTestIds.TenantId,
            DefaultTestIds.MerchantId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }
}
