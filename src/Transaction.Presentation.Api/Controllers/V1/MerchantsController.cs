using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Transaction.Application.Handlers;
using Transaction.Application.Handlers.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Observability;
using Transaction.Presentation.Api.Controllers.Base;

namespace Transaction.Presentation.Api.Controllers.V1;

/// <summary>
/// Controller for merchant-level queries.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants/{tenantId:guid}/[controller]")]
public sealed class MerchantsController(
    ILogger<MerchantsController> logger,
    IRequestHandler<GetDailySummaryQuery, DailySummaryResponse> getDailySummaryHandler) : BaseController(logger)
{
    /// <summary>
    /// Retrieves the daily transaction summary for a merchant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="merchantId">The merchant identifier.</param>
    /// <param name="date">The date for the summary.</param>
    [HttpGet("{merchantId}/daily-summary")]
    [ProducesResponseType(typeof(DailySummaryResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetDailySummaryAsync(
        Guid tenantId,
        string merchantId,
        [FromQuery] DateOnly date)
    {
        ObservabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        return await ProcessRequest(
            getDailySummaryHandler.HandleAsync,
            new GetDailySummaryQuery(tenantId, merchantId, date));
    }
}
