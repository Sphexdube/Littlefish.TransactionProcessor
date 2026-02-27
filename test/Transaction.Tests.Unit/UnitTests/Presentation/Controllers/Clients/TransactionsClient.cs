using Transaction.Application.Models.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Tests.Unit.Models;
using Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Clients;

public sealed class TransactionsClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<ApiResponse<IngestBatchResponse>> IngestBatch(Guid tenantId, IngestTransactionBatchRequest request)
    {
        return await PostAsync<IngestBatchResponse, IngestTransactionBatchRequest>(
            $"/api/v1/tenants/{tenantId}/transactions:ingest",
            request);
    }

    public async Task<ApiResponse<TransactionResponse>> GetTransaction(Guid tenantId, string transactionId)
    {
        return await GetAsync<TransactionResponse>(
            $"/api/v1/tenants/{tenantId}/transactions/{transactionId}");
    }
}
