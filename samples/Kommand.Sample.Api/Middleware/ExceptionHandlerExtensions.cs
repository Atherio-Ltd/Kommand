using Kommand;
using Kommand.Sample.Api.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace Kommand.Sample.Api.Middleware;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseKommandExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(HandleException);
        });
    }

    private static async Task HandleException(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ValidationException validationEx)
        {
            await WriteValidationErrorResponse(context, validationEx);
        }
        else if (exception is InvalidOperationException)
        {
            await WriteNotFoundResponse(context, exception);
        }
        else
        {
            await WriteInternalErrorResponse(context);
        }
    }

    private static async Task WriteValidationErrorResponse(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = new ErrorResponse(
            Title: "Validation Failed",
            Status: 400,
            Detail: "One or more validation errors occurred.",
            Errors: errors);

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static async Task WriteNotFoundResponse(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ErrorResponse(
            Title: "Not Found",
            Status: 404,
            Detail: ex.Message);

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static async Task WriteInternalErrorResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ErrorResponse(
            Title: "Internal Server Error",
            Status: 500,
            Detail: "An unexpected error occurred.");

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
