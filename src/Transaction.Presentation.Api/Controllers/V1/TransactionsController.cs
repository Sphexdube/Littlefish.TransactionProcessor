using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Transaction.Application.Constants;
using Transaction.Application.Handlers;
using Transaction.Application.Handlers.Request.V1;
using Transaction.Application.Models.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Observability;
using Transaction.Presentation.Api.Controllers.Base;

namespace Transaction.Presentation.Api.Controllers.V1;

/// <summary>
/// Controller for managing transaction ingestion and retrieval.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants/{tenantId:guid}/[controller]")]
public sealed class TransactionsController(
    ILogger<TransactionsController> logger,
    IRequestHandler<IngestBatchCommand, IngestBatchResponse> ingestHandler,
    IRequestHandler<GetTransactionQuery, TransactionResponse> getTransactionHandler,
    IValidator<IngestTransactionBatchRequest> validator) : BaseController(logger)
{
    /// <summary>
    /// Ingests a batch of transactions for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="request">The batch of transactions to ingest.</param>
    /// <param name="correlationId">The correlation identifier for tracking the request.</param>
    [HttpPost(":ingest")]
    [ProducesResponseType(typeof(IngestBatchResponse), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> IngestBatchAsync(
        Guid tenantId,
        [FromBody] IngestTransactionBatchRequest request,
        [FromHeader(Name = RequestHeaderKeys.CorrelationId)] string correlationId)
    {
        ObservabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        return await ProcessRequestAccepted(
            validator.ValidateAsync,
            (req, ct) => ingestHandler.HandleAsync(new IngestBatchCommand(tenantId, req, correlationId), ct),
            request,
            new Dictionary<string, string> { { RequestHeaderKeys.CorrelationId, correlationId } });
    }

    /// <summary>
    /// Retrieves a transaction by its ID.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    [HttpGet("{transactionId}")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionAsync(
        Guid tenantId,
        string transactionId)
    {
        ObservabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        return await ProcessRequest(
            getTransactionHandler.HandleAsync,
            new GetTransactionQuery(tenantId, transactionId));
    }
}
