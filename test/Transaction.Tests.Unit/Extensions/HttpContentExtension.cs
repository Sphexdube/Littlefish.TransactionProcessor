using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Transaction.Tests.Unit.Converters;
using Transaction.Tests.Unit.Models;

namespace Transaction.Tests.Unit.Extensions;

public static class HttpContentExtension
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new ExceptionMessageConverter() }
    };

    public static async Task<ApiResponse<TResponse>> DeserializeToApiResponse<TResponse>(
        this HttpContent content,
        HttpStatusCode statusCode)
    {
        bool isSuccess = (int)statusCode >= 200 && (int)statusCode < 300;
        string body = await content.ReadAsStringAsync();

        if (!isSuccess)
        {
            return new ApiResponse<TResponse>
            {
                StatusCode = (int)statusCode,
                IsError = true,
                ErrorBody = body
            };
        }

        TResponse? result = JsonSerializer.Deserialize<TResponse>(body, _options);

        return new ApiResponse<TResponse>
        {
            StatusCode = (int)statusCode,
            IsError = false,
            Result = result
        };
    }
}
