using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Transaction.Presentation.Api.Dependencies;

internal static class SwaggerRegistration
{
    internal static void Register(SwaggerGenOptions options)
    {
        string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
}
