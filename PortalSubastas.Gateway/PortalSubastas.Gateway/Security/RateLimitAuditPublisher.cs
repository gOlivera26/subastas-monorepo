using System.Security.Claims;
using System.Text.Json;
using MassTransit;
using PortalSubastas.Contracts.Events;

namespace PortalSubastas.Gateway.Security;

public interface IRateLimitAuditPublisher
{
    Task PublishRejectedAsync(HttpContext context, string correlationId, int retryAfterSeconds, string partitionKey, CancellationToken cancellationToken);
}

public sealed class RabbitMqRateLimitAuditPublisher : IRateLimitAuditPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RabbitMqRateLimitAuditPublisher> _logger;

    public RabbitMqRateLimitAuditPublisher(IPublishEndpoint publishEndpoint, ILogger<RabbitMqRateLimitAuditPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishRejectedAsync(HttpContext context, string correlationId, int retryAfterSeconds, string partitionKey, CancellationToken cancellationToken)
    {
        try
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
            var userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : (Guid?)null;
            var username = context.User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                username = userId?.ToString() ?? "Sistema";
            }

            var details = new
            {
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                QueryPresent = context.Request.QueryString.HasValue,
                RetryAfterSeconds = retryAfterSeconds,
                Partition = partitionKey,
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                CorrelationId = correlationId,
                Endpoint = context.GetEndpoint()?.DisplayName
            };

            var ev = new SystemLogEvent(
                UserId: userId,
                Username: username,
                Action: "SECURITY_RATE_LIMIT",
                Module: "GATEWAY",
                Details: JsonSerializer.Serialize(details),
                IpAddress: GetClientIp(context),
                OccurredAt: DateTime.UtcNow);

            await _publishEndpoint.Publish(ev, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo publicar evento SECURITY_RATE_LIMIT para {Path}", context.Request.Path.Value);
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(cfConnectingIp)
            ? cfConnectingIp
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public sealed class NoopRateLimitAuditPublisher : IRateLimitAuditPublisher
{
    private readonly ILogger<NoopRateLimitAuditPublisher> _logger;

    public NoopRateLimitAuditPublisher(ILogger<NoopRateLimitAuditPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishRejectedAsync(HttpContext context, string correlationId, int retryAfterSeconds, string partitionKey, CancellationToken cancellationToken)
    {
        _logger.LogDebug("SECURITY_RATE_LIMIT no publicado porque RabbitMQ no está configurado. Path={Path}", context.Request.Path.Value);
        return Task.CompletedTask;
    }
}
