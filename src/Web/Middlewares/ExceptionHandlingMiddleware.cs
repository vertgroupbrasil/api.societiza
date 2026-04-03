using Core.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;
using WorkflowTemplate.Domain.Exceptions;


namespace Web.Middlewares;

/// <summary>
/// Middleware to handle exceptions globally
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Validation failed",
                context,
                validationEx.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)).ToArray()),
            DuplicateEntityException duplicateEntityException => CreateErrorResponse(
                HttpStatusCode.Conflict,
                duplicateEntityException.Message,
                context),
            AggregateIsDeletedException aggregateIsDeletedException => CreateErrorResponse(
                HttpStatusCode.Conflict,
                aggregateIsDeletedException.Message,
                context),
            ArgumentException argumentException => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                argumentException.Message,
                context),
            UnauthorizedAccessException => CreateErrorResponse(
                HttpStatusCode.Unauthorized,
                "Unauthorized access",
                context),
            KeyNotFoundException keyNotFoundException => CreateErrorResponse(
                HttpStatusCode.NotFound,
                keyNotFoundException.Message,
                context),
            _ => CreateErrorResponse(
                HttpStatusCode.InternalServerError,
                exception.Message,
                context,
                exposeDetail: false)
        };

        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static ErrorResponse CreateErrorResponse(
        HttpStatusCode statusCode,
        string message,
        HttpContext context,
        ValidationError[]? errors = null,
        bool exposeDetail = true)
        => new(
            StatusCode: (int)statusCode,
            Message: message,
            Path: context.Request.Path.Value ?? string.Empty,
            TraceId: context.TraceIdentifier,
            Errors: errors,
            Detail: exposeDetail ? message : "An error occurred while processing your request");

    private sealed record ValidationError(string Field, string Error);

    private sealed record ErrorResponse(
        int StatusCode,
        string Message,
        string Path,
        string TraceId,
        ValidationError[]? Errors = null,
        string? Detail = null);
}
