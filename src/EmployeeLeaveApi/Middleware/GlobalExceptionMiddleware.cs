using System.Net;
using System.Text.Json;

namespace EmployeeLeaveApi.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred";

        // Log the error with more details
        _logger.LogError(exception,
            "❌ Error occurred: {Message} | Path: {Path} | Query: {Query} | Method: {Method} | User: {User} | IP: {Ip}",
            exception.Message,
            context.Request.Path,
            context.Request.QueryString,
            context.Request.Method,
            context.User?.Identity?.Name ?? "Anonymous",
            context.Connection.RemoteIpAddress);

        // Determine error type
        switch (exception)
        {
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Unauthorized access";
                break;
            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;
            case FluentValidation.ValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                message = "Validation Error";
                // You might want to serialize errors here, but for now we keep it simple or append to message
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Check environment for StackTrace
        var env = context.RequestServices.GetService<IWebHostEnvironment>();
        var isDev = env != null && env.IsDevelopment();

        var response = new
        {
            error = new
            {
                message,
                statusCode = (int)statusCode,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value,
                details = isDev ? exception.StackTrace : null // Only show stack trace in Dev
            }
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
