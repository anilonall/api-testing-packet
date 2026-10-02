using ApiTestingPacket.Core.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Infrastructure;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, title, errors) = exception switch
            {
                RequestValidationException value => (400, value.Message, value.Errors),
                ValidationException value => (400, "One or more validation errors occurred.", (IReadOnlyDictionary<string, string[]>)value.Errors.GroupBy(x => x.PropertyName).ToDictionary(x => char.ToLowerInvariant(x.Key[0]) + x.Key[1..], x => x.Select(e => e.ErrorMessage).ToArray())),
                NotFoundException => (404, exception.Message, null),
                ConflictException => (409, exception.Message, null),
                UnauthorizedAccessException or UnauthorizedException => (401, exception.Message, null),
                InvalidOperationException => (400, exception.Message, null),
                _ => (500, "An unexpected error occurred.", null)
            };
            if (status == 500) logger.LogError(exception, "Unhandled request error; trace {TraceId}", context.TraceIdentifier);
            var details = new ProblemDetails { Status = status, Title = title, Instance = context.Request.Path };
            details.Extensions["traceId"] = context.TraceIdentifier;
            if (errors is not null) details.Extensions["errors"] = errors;
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(details);
        }
    }
}
