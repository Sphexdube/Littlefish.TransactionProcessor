using System.Reflection;
using System.Text.Json.Serialization;

namespace Transaction.Presentation.Api.Dependencies;

internal static class ControllerRegistration
{
    internal static void Register(IServiceCollection services)
    {
        services
            .AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
    }
}
