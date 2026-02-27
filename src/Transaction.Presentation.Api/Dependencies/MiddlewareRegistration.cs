using Transaction.Presentation.Api.Middleware;

namespace Transaction.Presentation.Api.Dependencies;

internal static class MiddlewareRegistration
{
    internal static void Register(IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
