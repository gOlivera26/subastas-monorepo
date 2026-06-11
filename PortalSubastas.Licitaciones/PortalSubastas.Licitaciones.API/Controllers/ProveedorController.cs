using System.Data.Common;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/[controller]")]
public class ProveedorController : ControllerBase
{
    private readonly PortalSubastasContext _context;
    private readonly IHttpContextAccessor _http;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProveedorController> _logger;

    public ProveedorController(
        PortalSubastasContext context,
        IHttpContextAccessor http,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<ProveedorController> logger)
    {
        _context = context;
        _http = http;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int idCotizacion)
    {
        var list = await _context.TCotizacionProveedores
            .Where(p => p.IdCotizacion == idCotizacion && p.FecBaja == null)
            .Select(p => new { p.IdCotizacionProveedor, p.IdProveedor, p.Ganadora })
            .ToListAsync();
        return Ok(OperationResponse<object>.SuccessResponse(list));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int idCotizacion, [FromBody] ProveedorAddDto dto)
    {
        if (await _context.TCotizacionProveedores.AnyAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == dto.IdProveedor && p.FecBaja == null))
            return BadRequest(OperationResponse<object>.CustomErrorResponse(400, "El proveedor ya está asignado."));

        // Resolver email y nombre del proveedor desde negocio.t_proveedores
        var (email, nombre) = await GetProveedorDataAsync(dto.IdProveedor);
        if (email == null)
            return BadRequest(OperationResponse<object>.CustomErrorResponse(400, "Proveedor no encontrado o inactivo."));
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(OperationResponse<object>.CustomErrorResponse(400, "El proveedor no tiene email institucional configurado."));

        var entity = new TCotizacionProveedor
        {
            IdCotizacion = idCotizacion,
            IdProveedor = dto.IdProveedor,
            Ganadora = dto.Ganadora ?? "N",
            UsrIng = _http.HttpContext?.User?.Identity?.Name ?? "SISTEMA",
            FecIng = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };
        _context.TCotizacionProveedores.Add(entity);
        await _context.SaveChangesAsync();

        // Publicar evento async — try-catch para no interrumpir el request
        try
        {
            var cotizacion = await _context.TCotizaciones
                .Include(c => c.Especificacion)
                .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

            var tipoContratacion = await _context.TEstados.FindAsync(cotizacion?.IdTipoContratacion);
            string tipoNombre = cotizacion?.IdTipoContratacion switch
            {
                7 => "Subasta Inversa",
                9 => "Subasta Directa",
                13 => "Subasta Inversa Monto Fijo",
                15 => "Subasta Inversa SEEC",
                _ => "Subasta"
            };

            var evento = new ProveedorInvitadoEvent(
                IdCotizacion: idCotizacion,
                NroCotizacion: cotizacion?.NroCotizacion ?? "",
                Titulo: cotizacion?.Observacion ?? "Subasta " + (cotizacion?.NroCotizacion ?? ""),
                IdProveedor: dto.IdProveedor,
                EmailProveedor: email,
                NombreProveedor: nombre ?? "",
                FechaInicio: cotizacion?.Especificacion?.FechaInicioSubasta,
                FechaFin: cotizacion?.Especificacion?.FechaFinalizacionSubasta,
                TipoContratacion: tipoNombre,
                OccuredOn: DateTime.UtcNow
            );

            await _publishEndpoint.Publish(evento);
            _logger.LogInformation("📧 Evento ProveedorInvitadoEvent publicado: Cotización {IdCotizacion}, Proveedor {IdProveedor}", idCotizacion, dto.IdProveedor);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo publicar ProveedorInvitadoEvent para Cotización {IdCotizacion}, Proveedor {IdProveedor}. El request continúa.", idCotizacion, dto.IdProveedor);
        }

        return Ok(OperationResponse<object>.SuccessResponse(new { entity.IdCotizacionProveedor }));
    }

    private async Task<(string? Email, string? Nombre)> GetProveedorDataAsync(int idProveedor)
    {
        try
        {
            var dbConnection = _context.Database.GetDbConnection();
            await dbConnection.OpenAsync();
            await using var cmd = dbConnection.CreateCommand();
            cmd.CommandText = "SELECT email_institucional, razon_social FROM negocio.t_proveedores WHERE id = @id AND fec_baja IS NULL";
            var param = cmd.CreateParameter();
            param.ParameterName = "id";
            param.Value = idProveedor;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var email = reader.IsDBNull(0) ? null : reader.GetString(0);
                var nombre = reader.IsDBNull(1) ? null : reader.GetString(1);
                return (email, nombre);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error al consultar datos del proveedor {IdProveedor}", idProveedor);
        }

        return (null, null);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int idCotizacion, int id)
    {
        var entity = await _context.TCotizacionProveedores.FindAsync(id);
        if (entity == null || entity.IdCotizacion != idCotizacion)
            return NotFound();
        entity.FecBaja = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        entity.UsrBaja = _http.HttpContext?.User?.Identity?.Name ?? "SISTEMA";
        await _context.SaveChangesAsync();
        return Ok(OperationResponse<bool>.SuccessResponse(true));
    }
}

public class ProveedorAddDto
{
    public int IdProveedor { get; set; }
    public string? Ganadora { get; set; }
}
