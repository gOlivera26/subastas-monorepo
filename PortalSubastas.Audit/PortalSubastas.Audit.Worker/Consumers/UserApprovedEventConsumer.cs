using System.Text.Json;
using MassTransit;
using Npgsql;
using PortalSubastas.Contracts.Events;

namespace PortalSubastas.Audit.Worker.Consumers;

public class UserApprovedEventConsumer : IConsumer<UserApprovedEvent>
{
    private readonly ILogger<UserApprovedEventConsumer> _logger;
    private readonly IConfiguration _configuration;

    public UserApprovedEventConsumer(ILogger<UserApprovedEventConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<UserApprovedEvent> context)
    {
        var ev = context.Message;
        _logger.LogInformation("✅ Guardando en BD: El admin {AdminName} aprobó al usuario {UserId}", ev.AdminName, ev.UserId);

        string connString = _configuration.GetConnectionString("DefaultConnection")
                            ?? throw new Exception("Connection string not found");

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string sql = @"
            INSERT INTO auditoria.t_logs_eventos 
            (id_usuario, nombre_usuario, accion, modulo, detalles, ip_origen)
            VALUES (@idUsuario, @nombreAdmin, @accion, @modulo, @detalles::jsonb, @ipOrigen)";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idUsuario", ev.UserId);
        cmd.Parameters.AddWithValue("nombreAdmin", ev.AdminName);
        cmd.Parameters.AddWithValue("accion", ev.Action);
        cmd.Parameters.AddWithValue("modulo", ev.Module);

        cmd.Parameters.AddWithValue("ipOrigen", ev.IpAddress ?? (object)DBNull.Value);

        var detallesJson = JsonSerializer.Serialize(new
        {
            Mensaje = $"Cuenta aprobada el {ev.ApprovedAt:dd/MM/yyyy HH:mm:ss}"
        });
        cmd.Parameters.AddWithValue("detalles", detallesJson);

        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Registro de auditoría guardado exitosamente.");
    }
}