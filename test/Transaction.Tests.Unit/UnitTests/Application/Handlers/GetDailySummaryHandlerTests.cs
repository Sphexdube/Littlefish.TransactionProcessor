using Microsoft.Extensions.DependencyInjection;
using Transaction.Application.Handlers;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Application.Handlers;

public sealed class GetDailySummaryHandlerTests
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
    public async Task HandleAsync_ExistingSummary_ReturnsDailySummaryResponse()
    {
        // Arrange
        DateOnly summaryDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await DefaultDataSetup.AddMerchantDailySummary(
            _serviceProvider!,
            DefaultTestIds.MerchantId,
            summaryDate,
            totalAmount: 50_000m,
            transactionCount: 10);

        IRequestHandler<GetDailySummaryQuery, DailySummaryResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>>();

        GetDailySummaryQuery query = new GetDailySummaryQuery
        {
            TenantId = DefaultTestIds.TenantId,
            MerchantId = DefaultTestIds.MerchantId,
            Date = summaryDate
        };

        // Act
        DailySummaryResponse response = await handler.HandleAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.MerchantId, Is.EqualTo(DefaultTestIds.MerchantId));
            Assert.That(response.Date, Is.EqualTo(summaryDate));
            Assert.That(response.TransactionCount, Is.GreaterThan(0));
            Assert.That(response.TotalAmount, Is.GreaterThan(0));
            Assert.That(response.LastCalculatedAt, Is.Not.EqualTo(DateTimeOffset.MinValue));
        }
    }

    [Test]
    public void HandleAsync_NonExistentMerchantOrDate_ThrowsNotFoundException()
    {
        // Arrange
        IRequestHandler<GetDailySummaryQuery, DailySummaryResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>>();

        GetDailySummaryQuery query = new GetDailySummaryQuery
        {
            TenantId = DefaultTestIds.TenantId,
            MerchantId = "MERCHANT-DOES-NOT-EXIST",
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(query));
    }

    [Test]
    public void HandleAsync_WrongDate_ThrowsNotFoundException()
    {
        // Arrange
        IRequestHandler<GetDailySummaryQuery, DailySummaryResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>>();

        GetDailySummaryQuery query = new GetDailySummaryQuery
        {
            TenantId = DefaultTestIds.TenantId,
            MerchantId = DefaultTestIds.MerchantId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-999))
        };

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(query));
    }
}
