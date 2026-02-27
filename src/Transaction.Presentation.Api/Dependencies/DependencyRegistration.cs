namespace Transaction.Presentation.Api.Dependencies;

internal static class DependencyRegistration
{
    internal static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerDependencies();
        services.AddDatabaseDependencies(configuration);
        services.AddObservabilityDependencies();
        services.AddApplicationDependencies();
    }
}
