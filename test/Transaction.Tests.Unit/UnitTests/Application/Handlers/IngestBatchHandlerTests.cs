using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Transaction.Application.Constants;
using Transaction.Application.Handlers;
using Transaction.Application.Handlers.Request.V1;
using Transaction.Application.Models.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability.Contracts;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Application.Handlers;

public sealed class IngestBatchHandlerTests
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
    public async Task HandleAsync_ValidBatch_ReturnsAcceptedResponse()
    {
        // Arrange
        IRequestHandler<IngestBatchCommand, IngestBatchResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<IngestBatchCommand, IngestBatchResponse>>();

        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(5).Build();
        IngestBatchCommand command = request.BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId);

        // Act
        IngestBatchResponse response = await handler.HandleAsync(command);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.BatchId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(response.AcceptedCount, Is.EqualTo(5));
            Assert.That(response.RejectedCount, Is.EqualTo(0));
            Assert.That(response.QueuedCount, Is.EqualTo(5));
            Assert.That(response.CorrelationId, Is.EqualTo(DefaultTestIds.CorrelationId));
            Assert.That(response.Errors, Is.Null);
        }
    }

    [Test]
    public async Task HandleAsync_ValidBatch_PersistsTransactionsAndOutboxMessages()
    {
        // Arrange
        IRequestHandler<IngestBatchCommand, IngestBatchResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<IngestBatchCommand, IngestBatchResponse>>();

        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        IngestTransactionBatchRequest request = new IngestTransactionBatchRequestBuilder(3).Build();
        IngestBatchCommand command = request.BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId);

        // Act
        IngestBatchResponse response = await handler.HandleAsync(command);

        // Assert — verify DB state
        IEnumerable<TransactionRecord> transactions = await unitOfWork.Transactions.FindAsync(
            t => t.BatchId == response.BatchId);

        IEnumerable<OutboxMessage> outboxMessages = await unitOfWork.OutboxMessages.FindAsync(
            o => o.TenantId == DefaultTestIds.TenantId);

        Batch? batch = await unitOfWork.Batches.GetByIdAsync(response.BatchId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transactions.Count(), Is.EqualTo(3));
            Assert.That(outboxMessages.Count(), Is.EqualTo(3));
            Assert.That(batch, Is.Not.Null);
            Assert.That(batch!.AcceptedCount, Is.EqualTo(3));
            Assert.That(batch!.RejectedCount, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task HandleAsync_DuplicateTransactionId_ReturnsWithRejectionErrors()
    {
        // Arrange
        IRequestHandler<IngestBatchCommand, IngestBatchResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<IngestBatchCommand, IngestBatchResponse>>();

        TransactionItemRequest duplicateItem = new TransactionItemRequestBuilder("TXN-DUPLICATE").Build();

        // First batch — seeds the duplicate transaction
        IngestBatchCommand firstCommand = new IngestTransactionBatchRequest
        {
            Transactions = [duplicateItem]
        }.BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId);

        await handler.HandleAsync(firstCommand);

        // Second batch with same TransactionId
        IngestBatchCommand secondCommand = new IngestTransactionBatchRequest
        {
            Transactions = [duplicateItem]
        }.BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId);

        // Act
        IngestBatchResponse response = await handler.HandleAsync(secondCommand);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.AcceptedCount, Is.EqualTo(0));
            Assert.That(response.RejectedCount, Is.EqualTo(1));
            Assert.That(response.Errors, Is.Not.Null);
            Assert.That(response.Errors!.Count, Is.EqualTo(1));
            Assert.That(response.Errors![0].TransactionId, Is.EqualTo("TXN-DUPLICATE"));
            Assert.That(response.Errors![0].ErrorMessage, Is.EqualTo(ErrorMessages.DuplicateTransactionId));
        }
    }

    [Test]
    public async Task HandleAsync_PurchaseTransaction_PersistedWithCorrectTypeAndStatus()
    {
        // Arrange
        IRequestHandler<IngestBatchCommand, IngestBatchResponse> handler =
            _serviceProvider!.GetRequiredService<IRequestHandler<IngestBatchCommand, IngestBatchResponse>>();

        IUnitOfWork unitOfWork = _serviceProvider!.GetRequiredService<IUnitOfWork>();

        TransactionItemRequest item = new TransactionItemRequestBuilder("TXN-PURCHASE-001")
            .WithType("PURCHASE")
            .WithAmount(2500.00m)
            .WithCurrency("ZAR")
            .Build();

        IngestBatchCommand command = new IngestTransactionBatchRequest
        {
            Transactions = [item]
        }.BuildCommand(DefaultTestIds.TenantId, DefaultTestIds.CorrelationId);

        // Act
        IngestBatchResponse response = await handler.HandleAsync(command);

        // Assert
        TransactionRecord? transaction = (await unitOfWork.Transactions.FindAsync(
            t => t.TransactionId == "TXN-PURCHASE-001")).FirstOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction!.Type, Is.EqualTo(TransactionType.Purchase));
            Assert.That(transaction!.Status, Is.EqualTo(TransactionStatus.Received));
            Assert.That(transaction!.Amount, Is.EqualTo(2500.00m));
            Assert.That(transaction!.Currency, Is.EqualTo("ZAR"));
        }
    }

    [Test]
    public void HandleAsync_TenantNotFound_ThrowsNotFoundException()
    {
        // Arrange — use NSubstitute to mock the tenant lookup returning null
        ITenantRepository mockTenantRepo = Substitute.For<ITenantRepository>();
        mockTenantRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        IUnitOfWork mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.Tenants.Returns(mockTenantRepo);

        IngestBatchHandler handler = new(mockUnitOfWork, Substitute.For<IObservabilityManager>(), Substitute.For<IMetricRecorder>());

        IngestBatchCommand command = new IngestTransactionBatchRequest
        {
            Transactions = [new TransactionItemRequestBuilder().Build()]
        }.BuildCommand(Guid.NewGuid(), DefaultTestIds.CorrelationId);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
    }
}
