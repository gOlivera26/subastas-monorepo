using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;
using PortalSubastas.Licitaciones.Application.ResponseDto.Tablero;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public sealed class TableroService : ITableroService
{
    private const int EstadoFinalizada = 40;
    private const int CriterioItem = 0;
    private const int CriterioRenglon = 1;
    private const int TipoContratacionDirecta = 9;

    private readonly PortalSubastasContext _dbContext;
    private readonly IProviderLookupService _providerLookupService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TableroService(
        PortalSubastasContext context,
        IProviderLookupService providerLookupService,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = context;
        _providerLookupService = providerLookupService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<OperationResponse<TableroResponseDto>> GetAsync(
        int criterioAdjudicacion,
        int? idVigencia,
        CancellationToken cancellationToken = default)
    {
        if (criterioAdjudicacion is not (CriterioItem or CriterioRenglon))
            return OperationResponse<TableroResponseDto>.BadRequestResponse("El criterio de adjudicación debe ser 0 (Item) o 1 (Renglón).");

        var vigencia = idVigencia.HasValue
            ? await _dbContext.TVigencias.AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVigencia == idVigencia.Value && v.FecBaja == null, cancellationToken)
            : await _dbContext.TVigencias.AsNoTracking()
                .Where(v => v.FecBaja == null && v.ActivoEjecucion == true)
                .OrderByDescending(v => v.Ejercicio)
                .FirstOrDefaultAsync(cancellationToken);

        if (vigencia is null)
            return OperationResponse<TableroResponseDto>.BadRequestResponse("No se encontró una vigencia válida para el tablero.");

        var query = _dbContext.TCotizaciones
            .AsNoTracking()
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles.Where(d => d.FecBaja == null))
            .Include(c => c.Renglones.Where(r => r.FecBaja == null))
            .Include(c => c.Ofertas.Where(o => o.FecBaja == null))
            .Where(c =>
                c.FecBaja == null &&
                c.IdEstado == EstadoFinalizada &&
                c.IdVigencia == vigencia.IdVigencia &&
                c.Especificacion != null &&
                c.Especificacion.CriterioAdjudicacion == criterioAdjudicacion);

        var idOrganizacion = GetUserOrganizationId();
        if (!IsSuperAdmin() && idOrganizacion.HasValue)
            query = query.Where(c => c.IdOrganizacion == idOrganizacion.Value);

        var cotizaciones = await query.ToListAsync(cancellationToken);

        var cotizacionIds = cotizaciones.Select(c => c.IdCotizacion).ToList();
        var ganadores = cotizacionIds.Count == 0
            ? new List<TGanador>()
            : await _dbContext.TGanadores.AsNoTracking()
                .Where(g => g.FecBaja == null && cotizacionIds.Contains(g.IdCotizacion))
                .ToListAsync(cancellationToken);

        var itemIds = cotizaciones
            .SelectMany(c => c.Detalles)
            .Select(d => d.IdItem)
            .Distinct()
            .ToList();

        var items = itemIds.Count == 0
            ? new Dictionary<int, string>()
            : await _dbContext.TCatalogosBiens.AsNoTracking()
                .Where(i => itemIds.Contains(i.IdItem))
                .Select(i => new { i.IdItem, i.NItem })
                .ToDictionaryAsync(i => i.IdItem, i => i.NItem ?? $"Ítem #{i.IdItem}", cancellationToken);

        var providerIds = ganadores.Select(g => g.IdProveedor)
            .Concat(cotizaciones.SelectMany(c => c.Ofertas).Select(o => o.IdProveedor))
            .Distinct()
            .ToList();

        var providers = await _providerLookupService.GetByIdsAsync(providerIds, cancellationToken);

        var rows = criterioAdjudicacion == CriterioRenglon
            ? BuildRenglonRows(cotizaciones, ganadores, providers)
            : BuildItemRows(cotizaciones, ganadores, items, providers);

        var totalPresupuestado = rows.Sum(r => r.Presupuestado);
        var totalSubastado = rows.Sum(r => r.Subastado);
        var totalAhorrado = totalPresupuestado - totalSubastado;

        var response = new TableroResponseDto
        {
            IdVigencia = vigencia.IdVigencia,
            Ejercicio = vigencia.Ejercicio,
            CriterioAdjudicacion = criterioAdjudicacion,
            Criterio = criterioAdjudicacion == CriterioRenglon ? "Renglón" : "Item",
            TotalPresupuestado = Math.Round(totalPresupuestado, 2),
            TotalSubastado = Math.Round(totalSubastado, 2),
            TotalAhorrado = Math.Round(totalAhorrado, 2),
            PorcentajeAhorro = totalPresupuestado > 0 ? Math.Round(totalAhorrado / totalPresupuestado * 100, 2) : 0,
            CantidadCotizaciones = rows.Select(r => r.IdCotizacion).Distinct().Count(),
            CantidadFilas = rows.Count,
            Items = rows
        };

        return OperationResponse<TableroResponseDto>.SuccessResponse(response, rows.Count);
    }

    private static List<TableroItemDto> BuildItemRows(
        List<TCotizacion> cotizaciones,
        List<TGanador> ganadores,
        IReadOnlyDictionary<int, string> items,
        IReadOnlyDictionary<int, ProviderReportLookupDto> providers)
    {
        var rows = new List<TableroItemDto>();

        foreach (var cotizacion in cotizaciones)
        {
            foreach (var detalle in cotizacion.Detalles.OrderBy(d => d.IdCotizacionDetalle))
            {
                var presupuesto = detalle.ImporteBase * detalle.Cantidad;
                var ganador = ganadores
                    .Where(g => g.IdCotizacion == cotizacion.IdCotizacion && g.IdCotizacionDetalle == detalle.IdCotizacionDetalle)
                    .OrderBy(g => g.MontoGanador)
                    .FirstOrDefault();

                var fallback = ganador is null
                    ? GetBestOffer(cotizacion, detalle.IdCotizacionDetalle, null)
                    : null;

                var proveedorId = ganador?.IdProveedor ?? fallback?.IdProveedor ?? 0;
                var cantidad = ganador?.CantidadAdjudicada > 0 ? ganador.CantidadAdjudicada : detalle.Cantidad;
                var montoUnitario = ganador?.MontoGanador ?? fallback?.Monto ?? detalle.ImporteBase;
                var subastado = montoUnitario * cantidad;

                rows.Add(CreateRow(
                    cotizacion,
                    proveedorId,
                    providers,
                    detalle.IdCotizacionDetalle,
                    null,
                    items.GetValueOrDefault(detalle.IdItem, $"Ítem #{detalle.IdItem}"),
                    presupuesto,
                    subastado));
            }
        }

        return rows.OrderBy(r => r.Cotizacion).ThenBy(r => r.Item).ToList();
    }

    private static List<TableroItemDto> BuildRenglonRows(
        List<TCotizacion> cotizaciones,
        List<TGanador> ganadores,
        IReadOnlyDictionary<int, ProviderReportLookupDto> providers)
    {
        var rows = new List<TableroItemDto>();

        foreach (var cotizacion in cotizaciones)
        {
            foreach (var renglon in cotizacion.Renglones.OrderBy(r => r.NumeroRenglon))
            {
                var detallesRenglon = cotizacion.Detalles.Where(d => d.IdRenglon == renglon.IdRenglon).ToList();
                var presupuesto = detallesRenglon.Sum(d => d.ImporteBase * d.Cantidad);
                var ganador = ganadores
                    .Where(g => g.IdCotizacion == cotizacion.IdCotizacion && g.IdRenglon == renglon.IdRenglon)
                    .OrderBy(g => g.MontoGanador)
                    .FirstOrDefault();

                var fallback = ganador is null
                    ? GetBestOffer(cotizacion, null, renglon.IdRenglon)
                    : null;

                var proveedorId = ganador?.IdProveedor ?? fallback?.IdProveedor ?? 0;
                var subastado = ganador?.MontoGanador ?? fallback?.Monto ?? presupuesto;

                rows.Add(CreateRow(
                    cotizacion,
                    proveedorId,
                    providers,
                    null,
                    renglon.IdRenglon,
                    $"Renglón {renglon.NumeroRenglon} - {renglon.Descripcion}",
                    presupuesto,
                    subastado));
            }
        }

        return rows.OrderBy(r => r.Cotizacion).ThenBy(r => r.Item).ToList();
    }

    private static TOfertaSubasta? GetBestOffer(TCotizacion cotizacion, int? idDetalle, int? idRenglon)
    {
        var ofertas = cotizacion.Ofertas
            .Where(o =>
                (idDetalle.HasValue && o.IdCotizacionDetalle == idDetalle) ||
                (idRenglon.HasValue && o.IdRenglon == idRenglon))
            .ToList();

        if (ofertas.Count == 0)
            return null;

        return cotizacion.IdTipoContratacion == TipoContratacionDirecta
            ? ofertas.OrderByDescending(o => o.Monto).ThenBy(o => o.FechaOferta).First()
            : ofertas.OrderBy(o => o.Monto).ThenBy(o => o.FechaOferta).First();
    }

    private static TableroItemDto CreateRow(
        TCotizacion cotizacion,
        int proveedorId,
        IReadOnlyDictionary<int, ProviderReportLookupDto> providers,
        int? idDetalle,
        int? idRenglon,
        string item,
        decimal presupuestado,
        decimal subastado)
    {
        var ahorrado = presupuestado - subastado;

        return new TableroItemDto
        {
            IdCotizacion = cotizacion.IdCotizacion,
            Cotizacion = cotizacion.NroCotizacion,
            IdProveedor = proveedorId,
            Proveedor = proveedorId > 0 && providers.TryGetValue(proveedorId, out var provider)
                ? provider.RazonSocial
                : proveedorId > 0 ? $"Proveedor #{proveedorId}" : "Sin proveedor",
            IdCotizacionDetalle = idDetalle,
            IdRenglon = idRenglon,
            Item = item,
            Presupuestado = Math.Round(presupuestado, 2),
            Subastado = Math.Round(subastado, 2),
            Ahorrado = Math.Round(ahorrado, 2),
            PorcentajeAhorro = presupuestado > 0 ? Math.Round(ahorrado / presupuestado * 100, 2) : 0
        };
    }

    private int? GetUserOrganizationId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("IdOrganizacion");
        return claim != null && int.TryParse(claim.Value, out var orgId) ? orgId : null;
    }

    private bool IsSuperAdmin()
        => _httpContextAccessor.HttpContext?.User?.IsInRole("SUPERADMIN") == true;
}
