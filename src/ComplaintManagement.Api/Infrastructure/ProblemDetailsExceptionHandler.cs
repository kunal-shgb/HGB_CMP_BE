using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Infrastructure;

/// <summary>Maps application exceptions to RFC 7807 responses. Unexpected errors never leak details.</summary>
public sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetails, ILogger<ProblemDetailsExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ve => (ProblemDetails)new ValidationProblemDetails(
                ve.Errors.GroupBy(e => ToCamel(e.PropertyName)).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Please correct the highlighted fields.",
            },
            NotFoundException => new() { Status = StatusCodes.Status404NotFound, Title = "The requested record was not found." },
            ForbiddenAccessException fe => new() { Status = StatusCodes.Status403Forbidden, Title = fe.Message },
            DomainException de => new()
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = de.Message,
                Extensions = { ["code"] = de.Code },
            },
            _ => new() { Status = StatusCodes.Status500InternalServerError, Title = "We could not process this request. Please try again." },
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem, Exception = exception });
    }

    private static string ToCamel(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
