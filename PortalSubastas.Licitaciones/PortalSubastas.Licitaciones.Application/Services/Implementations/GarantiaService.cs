using PortalSubastas.Licitaciones.Application.RequestDto.Garantia;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Garantia;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class GarantiaService : BaseService, IGarantiaService
{
    private new readonly PortalSubastasContext _context;

    public GarantiaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<GarantiaResponseDto>>> GetByCotizacionAsync(int idCotizacion, int? idProveedor)
    {
        var query = _context.TGarantiasSubastas
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null);

        // Si es un proveedor, solo ve sus garantías. Si es Admin (idProveedor null), ve todas.
        if (idProveedor.HasValue && idProveedor.Value > 0)
        {
            query = query.Where(g => g.IdProveedor == idProveedor.Value);
        }

        var garantias = await query.OrderByDescending(g => g.FecIng).ToListAsync();
        return Ok(_mapper.Map<List<GarantiaResponseDto>>(garantias));
    }

    public async Task<OperationResponse<GarantiaResponseDto>> CreateAsync(GarantiaRequestDto dto)
    {
        if (dto.Archivo == null || dto.Archivo.Length == 0)
            return BadRequest<GarantiaResponseDto>("El archivo es obligatorio.");

        if (dto.Archivo.Length > 20 * 1024 * 1024)
            return BadRequest<GarantiaResponseDto>("El archivo no puede superar los 20 MB.");

        // TODO: Reemplazar con el IFileStorageService real conectado a S3/R2 o MinIO.
        var urlArchivo = $"https://storage.midominio.com/garantias/{Guid.NewGuid()}_{dto.Archivo.FileName}";

        var entity = new TGarantiaSubasta
        {
            IdCotizacion = dto.IdCotizacion,
            IdProveedor = dto.IdProveedor,
            IdTipoDocumento = dto.IdTipoDocumento,
            IdMoneda = dto.IdMoneda,
            CompaniaAseguradora = dto.CompaniaAseguradora,
            NroPoliza = dto.NroPoliza,
            MontoCaucion = dto.MontoCaucion,
            MontoPagare = dto.MontoPagare,
            FechaPagare = dto.FechaPagare.HasValue ? DateOnly.FromDateTime(dto.FechaPagare.Value) : null,
            Observacion = dto.Observacion,
            NombreArchivo = dto.Archivo.FileName,
            UrlArchivo = urlArchivo
        };

        PrepareAuditableEntity(entity, isNew: true);

        _context.TGarantiasSubastas.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<GarantiaResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TGarantiasSubastas.FindAsync(id);
        if (entity == null) return NotFound<bool>();

        // Usamos la función DeleteAsync heredada de BaseService que maneja el Soft Delete (FecBaja/UsrBaja)
        return await DeleteAsync(entity, _context);
    }
}