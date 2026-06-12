using Microsoft.EntityFrameworkCore;
using PortalSubastas.Licitaciones.Application.RequestDto.Proveedor;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class ProveedorService : BaseService, IProveedorService
{
    private readonly PortalSubastasContext _context;
    private readonly IProveedorRepresentanteService _proveedorRepresentanteService;

    public ProveedorService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IProveedorRepresentanteService proveedorRepresentanteService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _proveedorRepresentanteService = proveedorRepresentanteService;
    }

    public async Task<OperationResponse<object>> AddProveedorAsync(int idCotizacion, ProveedorAddDto dto)
    {
        // Validar que el proveedor no esté ya asignado
        if (await _context.TCotizacionProveedores.AnyAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == dto.IdProveedor && p.FecBaja == null))
            return BadRequest<object>("El proveedor ya está asignado.");

        // Validar que el proveedor tenga representantes con email
        var representantes = await _proveedorRepresentanteService.GetRepresentantesAsync(dto.IdProveedor);
        if (representantes.Count == 0)
            return BadRequest<object>("El proveedor no tiene representantes con email configurado.");

        // Crear entidad
        var entity = new TCotizacionProveedor
        {
            IdCotizacion = idCotizacion,
            IdProveedor = dto.IdProveedor,
            Ganadora = dto.Ganadora ?? "N",
            UsrIng = GetCurrentUsername(),
            FecIng = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };
        _context.TCotizacionProveedores.Add(entity);
        await _context.SaveChangesAsync();

        // Los correos NO se envían acá. Se envían al publicar la subasta
        // (NotificarAsync → PublishSubastaPublicadaEventAsync).

        return Ok<object>(new { entity.IdCotizacionProveedor });
    }
}
