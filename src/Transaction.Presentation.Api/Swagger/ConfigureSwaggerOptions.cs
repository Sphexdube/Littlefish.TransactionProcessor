using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Transaction.Presentation.Api.Swagger;

/// <summary>
/// Creates one Swagger document per discovered API version so that Swashbuckle
/// substitutes the real version number in route templates instead of showing {version}.
/// </summary>
internal sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Transaction Processor API",
                Version = description.ApiVersion.ToString()
            });
        }
    }
}
