using Elegance.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Middleware;

public sealed class ApplicationFailureExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ApplicationFailure failure)
        {
            return false;
        }

        var (status, title) = failure.Kind switch
        {
            FailureKind.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            FailureKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            FailureKind.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status400BadRequest, "Bad request"),
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = failure.Message },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
