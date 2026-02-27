using Microsoft.Extensions.DependencyInjection;
using Transaction.Application.Handlers;
using Transaction.Application.Models.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Application.Handlers;

public sealed class GetTransactionHandlerTests
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
    public async Task HandleAsync_ExistingTransaction_ReturnsTransactionResponse()
    {
        // Arrange — first ingest a transaction so it exists
        IRequestHandler<IngestBatchCommand, IngestBatchResponse> ingestHandler =
            _serviceProvider!.GetRequiredService<IRequestHandler<IngestBatchCommand, IngestBatchResponse>>();

        IRequestHandler<GetTransactionQuery, TransactionResponse> getHandler =
            _serviceProvider!.GetRequiredService<IRequestHandler<GetTransactionQuery, TransactionResponse>>();

        const string transactionId = "TXN-GET-001";
        TransactionItemRequest item = new TransactionItemRequestBuilder(transactionId)
            .WithAmount(3000.00m)
            .WithCurrency("ZAR")
            .WithMerchantId("MERCHANT-GET")
            .Build();

        await ingestHandler.HandleAsync(
            new IngestTransactionBatchRequest { Transactions = [item] }
                .BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId));

        GetTransactionQuery query = new GetTransactionQuery
        {
            TenantId = DefaultTestIds.TenantId,
            TransactionId = transactionId
        };

        // Act
        TransactionResponse response = await getHandler.HandleAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.TransactionId, Is.EqualTo(transactionId));
            Assert.That(response.MerchantId, Is.EqualTo("MERCHANT-GET"));
            Assert.That(response.Amount, Is.EqualTo(3000.00m));
            Assert.That(response.Currency, Is.EqualTo("ZAR"));
            Assert.That(response.Type, Is.EqualTo("PURCHASE"));
            Assert.That(response.Status, Is.EqualTo("RECEIVED"));
            Assert.That(response.Id, Is.Not.EqualTo(Guid.Empty));
        }
    }

    [Test]
    public void HandleAsync_NonExistentTransaction_ThrowsNotFoundException()
    {
        // Arrange
        IRequestHandler<GetTransactionQuery, TransactionResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<GetTransactionQuery, TransactionResponse>>();

        GetTransactionQuery query = new GetTransactionQuery
        {
            TenantId = DefaultTestIds.TenantId,
            TransactionId = "TXN-DOES-NOT-EXIST"
        };

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(query));
    }

    [Test]
    public async Task HandleAsync_RefundTransaction_ReturnsCorrectType()
    {
        // Arrange
        IRequestHandler<IngestBatchCommand, IngestBatchResponse> ingestHandler =
            _serviceProvider!.GetRequiredService<IRequestHandler<IngestBatchCommand, IngestBatchResponse>>();

        IRequestHandler<GetTransactionQuery, TransactionResponse> getHandler =
            _serviceProvider!.GetRequiredService<IRequestHandler<GetTransactionQuery, TransactionResponse>>();

        const string originalTxnId = "TXN-ORIGINAL-001";
        const string refundTxnId = "TXN-REFUND-001";

        IList<TransactionItemRequest> items =
        [
            new TransactionItemRequestBuilder(originalTxnId).WithType("PURCHASE").Build(),
            new TransactionItemRequestBuilder(refundTxnId)
                .WithType("REFUND")
                .WithOriginalTransactionId(originalTxnId)
                .Build()
        ];

        await ingestHandler.HandleAsync(
            new IngestTransactionBatchRequest { Transactions = items }
                .BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId));

        GetTransactionQuery query = new GetTransactionQuery
        {
            TenantId = DefaultTestIds.TenantId,
            TransactionId = refundTxnId
        };

        // Act
        TransactionResponse response = await getHandler.HandleAsync(query);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Type, Is.EqualTo("REFUND"));
            Assert.That(response.OriginalTransactionId, Is.EqualTo(originalTxnId));
        }
    }
}
