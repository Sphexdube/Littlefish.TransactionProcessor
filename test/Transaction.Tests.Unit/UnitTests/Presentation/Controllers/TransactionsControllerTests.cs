using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Transaction.Application.Models.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;
using Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers;

public sealed class TransactionsControllerTests : BaseControllerTests
{
    [SetUp]
    public async Task SeedData()
    {
        await DefaultDataSetup.AddDefaultTenant(ServiceProvider!);
    }

    [Test]
    public async Task IngestBatchAsync_ValidRequest_Returns202Accepted()
    {
        // Arrange
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(100).Build();

        // Act
        ApiResponse<IngestBatchResponse> response =
            await TransactionsClient!.IngestBatch(DefaultTestIds.TenantId, request);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.Accepted));
            Assert.That(response.IsError, Is.False);
            Assert.That(response.Result, Is.Not.Null);
            Assert.That(response.Result!.BatchId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(response.Result!.AcceptedCount, Is.EqualTo(100));
            Assert.That(response.Result!.RejectedCount, Is.EqualTo(0));
            Assert.That(response.Result!.QueuedCount, Is.EqualTo(100));
        }
    }

    [Test]
    public async Task IngestBatchAsync_EmptyTransactions_Returns400BadRequest()
    {
        // Arrange
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder()
            .WithTransactions([])
            .Build();

        // Act
        ApiResponse<IngestBatchResponse> response =
            await TransactionsClient!.IngestBatch(DefaultTestIds.TenantId, request);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(response.IsError, Is.True);
        }
    }

    [Test]
    public async Task IngestBatchAsync_BatchBelowMinimumSize_Returns400BadRequest()
    {
        // Arrange
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(50).Build();

        // Act
        ApiResponse<IngestBatchResponse> response =
            await TransactionsClient!.IngestBatch(DefaultTestIds.TenantId, request);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(response.IsError, Is.True);
        }
    }

    [Test]
    public async Task IngestBatchAsync_UnknownTenant_Returns404NotFound()
    {
        // Arrange
        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(100).Build();
        Guid unknownTenantId = Guid.NewGuid();

        // Act
        ApiResponse<IngestBatchResponse> response =
            await TransactionsClient!.IngestBatch(unknownTenantId, request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetTransactionAsync_ExistingTransaction_Returns200OK()
    {
        // Arrange — ingest a transaction first via the API
        IngestTransactionBatchRequest ingestRequest = new IngestTransactionBatchRequestBuilder()
            .WithTransactions([new TransactionItemRequestBuilder("TXN-CTRL-001").Build()])
            .Build();

        // Override batch size constraint by using 100 items including our target
        IList<TransactionItemRequest> transactions =
        [
            new TransactionItemRequestBuilder("TXN-CTRL-GET-001")
                .WithMerchantId("MERCHANT-CTRL")
                .WithAmount(7500m)
                .Build(),
            .. IngestTransactionBatchRequestBuilder.GenerateTransactions(99)
        ];

        IngestTransactionBatchRequest fullRequest = new IngestTransactionBatchRequestBuilder()
            .WithTransactions(transactions)
            .Build();

        await TransactionsClient!.IngestBatch(DefaultTestIds.TenantId, fullRequest);

        // Act
        ApiResponse<TransactionResponse> response =
            await TransactionsClient!.GetTransaction(DefaultTestIds.TenantId, "TXN-CTRL-GET-001");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(response.IsError, Is.False);
            Assert.That(response.Result, Is.Not.Null);
            Assert.That(response.Result!.TransactionId, Is.EqualTo("TXN-CTRL-GET-001"));
            Assert.That(response.Result!.MerchantId, Is.EqualTo("MERCHANT-CTRL"));
            Assert.That(response.Result!.Amount, Is.EqualTo(7500m));
        }
    }

    [Test]
    public async Task GetTransactionAsync_NonExistentTransaction_Returns404NotFound()
    {
        // Act
        ApiResponse<TransactionResponse> response =
            await TransactionsClient!.GetTransaction(DefaultTestIds.TenantId, "TXN-GHOST-999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }
}
