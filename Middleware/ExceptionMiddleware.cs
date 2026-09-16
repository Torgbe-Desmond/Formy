using System.Text.Json;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using ValidationException = Formify.Api.Exceptions.ValidationException;

namespace Formify.Api.Middleware;

public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (ValidationException vex)
        {
            await WriteAsync(context, vex.StatusCode, new ErrorMessage(vex.Message));
        }
        catch (AppException aex)
        {
            await WriteAsync(context, aex.StatusCode, new ErrorMessage(aex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, 500, new ErrorMessage(ex.ToString()));
        }
    }

    private static Task WriteAsync(HttpContext context, int statusCode, ErrorMessage payload)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        ErrorResponseModel errorResponseModel = new ErrorResponseModel
        {
            Message = payload.message,
            StatusCode = statusCode,
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponseModel, JsonOptions));
    }
}
