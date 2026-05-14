using MassTransit;
using Npgsql;
using PortalSubastas.Contracts.Events;

namespace PortalSubastas.Audit.Worker.Consumers;

public class DataChangedEventConsumer : IConsumer<DataChangedEvent>
{
    private readonly ILogger<DataChangedEventConsumer> _logger;
    private readonly IConfiguration _configuration;

    public DataChangedEventConsumer(ILogger<DataChangedEventConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<DataChangedEvent> context)
    {
        var ev = context.Message;
        _logger.LogInformation("📝 Insertando auditoría de datos: {Tabla} | {Operacion} | {Registro}", ev.TableName, ev.OperationType, ev.RecordId);

        string connString = _configuration.GetConnectionString("DefaultConnection")
                            ?? throw new Exception("Connection string not found");

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string sql = @"
            INSERT INTO auditoria.t_auditoria_datos
                (fecha_hora, id_usuario, tabla_afectada, registro_id, tipo_operacion, valores_anteriores, valores_nuevos)
            VALUES
                (@fechaHora, @idUsuario, @tabla, @registroId, @operacion, @valoresAnteriores::jsonb, @valoresNuevos::jsonb)";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fechaHora", ev.OccurredAt);
        cmd.Parameters.AddWithValue("idUsuario", ev.UserId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("tabla", ev.TableName);
        cmd.Parameters.AddWithValue("registroId", ev.RecordId);
        cmd.Parameters.AddWithValue("operacion", ev.OperationType);
        cmd.Parameters.AddWithValue("valoresAnteriores", ev.OldValues ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("valoresNuevos", ev.NewValues ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Auditoría de datos guardada para {Tabla}/{Registro}", ev.TableName, ev.RecordId);
    }
}
