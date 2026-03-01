using Microsoft.AspNetCore.Mvc;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;

namespace Transaction.Presentation.Api.Controllers.Base;

[ApiController]
public abstract class BaseController(ILogger logger) : ControllerBase
{
    protected IObservabilityManager ObservabilityManager { get; } = new ObservabilityManager(logger);

    protected async Task<IActionResult> ProcessRequest<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> handle,
        TRequest request)
    {
        var response = await handle(request, HttpContext.RequestAborted);
        return Ok(response);
    }

    protected async Task<IActionResult> ProcessRequestAccepted<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<FluentValidation.Results.ValidationResult>> validate,
        Func<TRequest, CancellationToken, Task<TResponse>> handle,
        TRequest request)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var validationResult = await validate(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var response = await handle(request, cancellationToken);
        return Accepted(response);
    }
}
