using System.Text.Json;
using MassTransit;
using Npgsql;
using PortalSubastas.Contracts.Events;

namespace PortalSubastas.Audit.Worker.Consumers;

public class SystemLogEventConsumer : IConsumer<SystemLogEvent>
{
    private readonly ILogger<SystemLogEventConsumer> _logger;
    private readonly IConfiguration _configuration;

    public SystemLogEventConsumer(ILogger<SystemLogEventConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<SystemLogEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("✅ Guardando Log de Sistema: [{Module}] {Action} por {Username}", msg.Module, msg.Action, msg.Username);

        string connString = _configuration.GetConnectionString("DefaultConnection")
                            ?? throw new Exception("Connection string not found");

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string sql = @"
            INSERT INTO auditoria.t_logs_eventos 
            (id_usuario, nombre_usuario, accion, modulo, detalles, ip_origen, fecha_hora)
            VALUES (@idUsuario, @nombreUsuario, @accion, @modulo, @detalles::jsonb, @ipOrigen, @fechaHora)";

        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("idUsuario", msg.UserId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("nombreUsuario", msg.Username ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("accion", msg.Action);
        cmd.Parameters.AddWithValue("modulo", msg.Module);

        cmd.Parameters.AddWithValue("detalles", string.IsNullOrWhiteSpace(msg.Details) ? "{}" : msg.Details);
        cmd.Parameters.AddWithValue("ipOrigen", msg.IpAddress ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("fechaHora", msg.OccurredAt);

        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Log de sistema guardado exitosamente en BD.");
    }
}