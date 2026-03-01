using Asp.Versioning.ApiExplorer;

namespace Transaction.Presentation.Api.Dependencies;

internal static class FeatureRegistration
{
    internal static void Register(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsEnvironment("Localhost"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (ApiVersionDescription description in app.DescribeApiVersions())
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }
            });
        }

        app.UseHttpsRedirection();
    }
}
