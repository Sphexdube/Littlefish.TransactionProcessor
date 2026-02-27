using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;

namespace Transaction.Presentation.Api.Middleware;

public class ExceptionHandlingMiddleware(IObservabilityManager observabilityManager, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        Exception baseException = exception.GetBaseException();
        switch (baseException)
        {
            case NotFoundException:
                observabilityManager.LogMessage(baseException.Message).AsError();
                await HandleNotFoundResponseAsync(context, baseException);
                break;
            default:
                throw exception;
        }
    }

    private static async Task HandleNotFoundResponseAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync(exception.Message);
    }
}
