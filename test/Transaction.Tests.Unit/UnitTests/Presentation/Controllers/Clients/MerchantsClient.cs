using Transaction.Application.Models.Response.V1;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Clients;

public sealed class MerchantsClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<ApiResponse<DailySummaryResponse>> GetDailySummary(Guid tenantId, string merchantId, DateOnly date)
    {
        return await GetAsync<DailySummaryResponse>(
            $"/api/v1/tenants/{tenantId}/merchants/{merchantId}/daily-summary?date={date:yyyy-MM-dd}");
    }
}
