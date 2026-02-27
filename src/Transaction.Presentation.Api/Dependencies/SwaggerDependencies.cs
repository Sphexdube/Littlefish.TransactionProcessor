using Asp.Versioning;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Transaction.Presentation.Api.Swagger;

namespace Transaction.Presentation.Api.Dependencies;

internal static class SwaggerDependencies
{
    internal static IServiceCollection AddSwaggerDependencies(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(SwaggerRegistration.Register);

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc()
          .AddApiExplorer(options =>
          {
              options.GroupNameFormat = "'v'VVV";
              options.SubstituteApiVersionInUrl = true;
          });

        return services;
    }
}
