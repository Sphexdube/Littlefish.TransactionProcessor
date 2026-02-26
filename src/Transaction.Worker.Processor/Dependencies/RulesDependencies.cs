using Transaction.Domain.Rules;

namespace Transaction.Worker.Processor.Dependencies;

internal static class RulesDependencies
{
    internal static IServiceCollection AddRulesDependencies(this IServiceCollection services)
    {
        services.AddScoped<IRuleEngine, RulesEngineAdapter>();

        return services;
    }
}
