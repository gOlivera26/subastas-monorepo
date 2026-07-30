namespace PortalSubastas.Reporting.API.Middlewares;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception in Reporting API");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context.TraceIdentifier;
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            var response = OperationResponse<object>.CreateBuilder()
                .WithCode(StatusCodes.Status500InternalServerError)
                .WithMessage("Ocurrió un error interno en el microservicio de reportería.")
                .WithData(new { correlationId })
                .Build();

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
