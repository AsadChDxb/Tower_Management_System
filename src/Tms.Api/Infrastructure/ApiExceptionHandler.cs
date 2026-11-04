using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tms.Domain.Common;

namespace Tms.Api.Infrastructure;

/// <summary>
/// Translates thrown exceptions into RFC 7807 problem responses so every error the React app
/// sees has the same shape: status, title, detail and a machine-readable <c>code</c>.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title, string Code)? mapped = exception switch
        {
            DomainRuleException domain => (
                domain.IsConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest,
                domain.IsConflict ? "Business rule violated" : "Invalid request",
                domain.RuleCode),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "unauthorized"),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not found",
                "not_found"),
            _ => null
        };

        if (mapped is null)
        {
            return false;
        }

        var (status, title, code) = mapped.Value;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Request failed with {Status}", status);
        }
        else
        {
            _logger.LogInformation("Request failed with {Status} ({Code}): {Message}", status, code, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.io/{status}"
        };

        problem.Extensions["code"] = code;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }
}
