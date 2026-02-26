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
            await HandleExceptionMessages(context, ex);
        }
    }

    private async Task HandleExceptionMessages(HttpContext context, Exception exception)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        Exception baseException = exception.GetBaseException();
        switch (baseException)
        {
            case NotFoundException:
                await HandleNotFoundResponse(context, baseException);
                break;
            default:
                throw exception;
        }
    }

    private async Task HandleNotFoundResponse(HttpContext context, Exception exception)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync(exception.Message);
    }
}
