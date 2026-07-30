namespace PortalSubastas.Identity.API.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, _environment);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, IHostEnvironment environment)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context.TraceIdentifier;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var response = OperationResponse<object>.CreateBuilder()
            .WithSuccess(false)
            .WithCode(StatusCodes.Status500InternalServerError)
            .WithMessage("Ha ocurrido un error inesperado, si el error persiste comuníquese con soporte.")
            .WithData(new { correlationId });

        if (environment.IsDevelopment())
        {
            response.WithException(exception.Message, new
            {
                exception.StackTrace,
                InnerException = exception.InnerException?.Message
            });
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(response.Build()));
    }
}
