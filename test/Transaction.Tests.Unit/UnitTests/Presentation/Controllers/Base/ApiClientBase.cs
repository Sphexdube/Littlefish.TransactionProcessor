using System.Net.Http.Json;
using Transaction.Tests.Unit.Extensions;
using Transaction.Tests.Unit.Models;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

public abstract class ApiClientBase(HttpClient httpClient)
{
    protected HttpClient HttpClient { get; } = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    protected async Task<ApiResponse<TResponse>> PostAsync<TResponse, TRequest>(string url, TRequest request)
    {
        HttpResponseMessage responseMessage = await HttpClient.PostAsJsonAsync(url, request);
        return await responseMessage.Content.DeserializeToApiResponse<TResponse>(responseMessage.StatusCode);
    }

    protected async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string url)
    {
        HttpResponseMessage responseMessage = await HttpClient.GetAsync(url);
        return await responseMessage.Content.DeserializeToApiResponse<TResponse>(responseMessage.StatusCode);
    }
}
