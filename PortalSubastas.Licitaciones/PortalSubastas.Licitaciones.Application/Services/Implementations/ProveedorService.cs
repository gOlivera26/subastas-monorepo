using System.Collections.Concurrent;
using System.Data;
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

    public ProveedorService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IProveedorRepresentanteService proveedorRepresentanteService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _proveedorRepresentanteService = proveedorRepresentanteService;
    }

    public async Task<OperationResponse<object>> AddProveedorAsync(int idCotizacion, ProveedorAddDto dto)
    {
        if (!IsSuperAdmin()) return Unauthorized<object>();

        return await AuctionProviderMutationLock.ExecuteAsync(_context, idCotizacion, async () =>
        {
            var auction = await GetAuctionAsync(idCotizacion);
            if (auction is null) return NotFound<object>();
            if (auction.IdEstado != 4) return BadRequest<object>("Solo se pueden asignar proveedores a una subasta en estado Generado.");
            if (await _context.TCotizacionProveedores.AnyAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == dto.IdProveedor && p.FecBaja == null)) return BadRequest<object>("El proveedor ya está asignado.");

            var representatives = await _proveedorRepresentanteService.GetRepresentantesAsync(dto.IdProveedor);
            if (representatives.Count == 0) return BadRequest<object>("El proveedor no tiene representantes con email configurado.");

            var entity = new TCotizacionProveedor { IdCotizacion = idCotizacion, IdProveedor = dto.IdProveedor, Ganadora = dto.Ganadora ?? "N" };
            PrepareAuditableEntity(entity, isNew: true);
            _context.TCotizacionProveedores.Add(entity);
            await _context.SaveChangesAsync();
            return Ok<object>(new { entity.IdCotizacionProveedor });
        });
    }

    public async Task<OperationResponse<bool>> RemoveProveedorAsync(int idCotizacion, int idCotizacionProveedor)
    {
        if (!IsSuperAdmin()) return Unauthorized<bool>();

        return await AuctionProviderMutationLock.ExecuteAsync(_context, idCotizacion, async () =>
        {
            var auction = await GetAuctionAsync(idCotizacion);
            if (auction is null) return NotFound<bool>();

            var relation = await _context.TCotizacionProveedores.FirstOrDefaultAsync(p => p.IdCotizacionProveedor == idCotizacionProveedor && p.IdCotizacion == idCotizacion && p.FecBaja == null);
            if (relation is null) return NotFound<bool>();
            if (auction.IdEstado != 4) return BadRequest<bool>("Solo se pueden remover proveedores de una subasta en estado Generado.");

            PrepareAuditableEntity(relation, isNew: false, isDeleted: true);
            await _context.SaveChangesAsync();
            return Ok(true);
        });
    }

    private Task<TCotizacion?> GetAuctionAsync(int idCotizacion) => _context.TCotizaciones.FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion && c.FecBaja == null);
}

internal static class AuctionProviderMutationLock
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> InProcessLocks = new();

    public static async Task<T> ExecuteAsync<T>(PortalSubastasContext context, int idCotizacion, Func<Task<T>> action)
    {
        var gate = InProcessLocks.GetOrAdd(idCotizacion, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (!context.Database.IsRelational())
                return await action();

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM negocio.t_cotizacion WHERE id_cotizacion = {idCotizacion} FOR UPDATE");
            var result = await action();
            await transaction.CommitAsync();
            return result;
        }
        finally
        {
            gate.Release();
        }
    }
}
