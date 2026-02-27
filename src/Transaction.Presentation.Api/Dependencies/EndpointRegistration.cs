namespace Transaction.Presentation.Api.Dependencies;

internal static class EndpointRegistration
{
    internal static void Register(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
    }
}
