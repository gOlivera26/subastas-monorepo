using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.OfertaSubasta;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.OfertaSubasta;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class OfertaSubastaService : BaseService, IOfertaSubastaService
{
    private new readonly PortalSubastasContext _context;
    private readonly ISubastaNotificationService _notificationService;

    public OfertaSubastaService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        ISubastaNotificationService notificationService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<OperationResponse<List<OfertaItemResponseDto>>> ProcesarOfertasAsync(int idCotizacion, List<OfertaItemRequestDto> ofertas)
    {
        var idProveedor = GetUserProveedorId();
        if (!idProveedor.HasValue)
            return BadRequest<List<OfertaItemResponseDto>>("No se pudo identificar al proveedor.");

        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion == null)
            return NotFound<List<OfertaItemResponseDto>>();

        if (cotizacion.IdEstado != 39) // 39 = Enviada Pendiente (En Curso)
            return BadRequest<List<OfertaItemResponseDto>>("La subasta no se encuentra en curso.");

        var ahora = DateTime.Now;
        if (ahora < cotizacion.Especificacion?.FechaInicioSubasta)
            return BadRequest<List<OfertaItemResponseDto>>("La subasta aún no ha comenzado.");
        if (ahora > cotizacion.Especificacion?.FechaFinalizacionSubasta)
            return BadRequest<List<OfertaItemResponseDto>>("La subasta ya ha finalizado.");

        // VALIDACIÓN ESTRICTA DE GARANTÍAS PREVIAS
        bool tieneGarantia = await _context.TGarantiasSubastas
            .AnyAsync(g => g.IdCotizacion == idCotizacion && g.IdProveedor == idProveedor.Value && g.FecBaja == null);

        if (!tieneGarantia)
        {
            return BadRequest<List<OfertaItemResponseDto>>("Para poder ofertar debe subir la garantía y/o pagaré correspondiente.");
        }

        var detallesIds = cotizacion.Detalles.Select(d => d.IdReservaDetalle).ToList();
        var monedasDic = await _context.TReservaDetalles
            .Where(r => detallesIds.Contains(r.IdReservaDet))
            .ToDictionaryAsync(r => r.IdReservaDet, r => r.IdMoneda);

        var resultados = new List<OfertaItemResponseDto>();
        var ofertasValidas = new List<TOfertaSubasta>();
        var margenMejora = cotizacion.Especificacion?.MargenMejora ?? 0;

        foreach (var oferta in ofertas)
        {
            var resultItem = new OfertaItemResponseDto
            {
                IdCotizacionDetalle = oferta.IdCotizacionDetalle,
                IdRenglon = oferta.IdRenglon
            };

            decimal mejorOfertaActual = 0;
            decimal precioBase = 0;

            int? idMonedaRequerida = null;
            if (oferta.IdRenglon.HasValue)
            {
                var det = cotizacion.Detalles.FirstOrDefault(d => d.IdRenglon == oferta.IdRenglon);
                if (det != null && monedasDic.TryGetValue(det.IdReservaDetalle, out var mId))
                    idMonedaRequerida = mId;
            }
            else if (oferta.IdCotizacionDetalle.HasValue)
            {
                var det = cotizacion.Detalles.FirstOrDefault(d => d.IdCotizacionDetalle == oferta.IdCotizacionDetalle);
                if (det != null && monedasDic.TryGetValue(det.IdReservaDetalle, out var mId))
                    idMonedaRequerida = mId;
            }

            if (idMonedaRequerida.HasValue && oferta.IdMonedaOferta != idMonedaRequerida.Value)
            {
                resultItem.TextoError = "La moneda de la oferta no coincide con la moneda oficial estipulada en el pliego.";
                resultados.Add(resultItem);
                continue; // Frenamos esta oferta y seguimos con la siguiente
            }

            // 1. Obtener la Mejor Oferta Actual (Histórica) y el Precio Base
            if (oferta.IdRenglon.HasValue)
            {
                precioBase = cotizacion.Detalles.Where(d => d.IdRenglon == oferta.IdRenglon).Sum(d => d.ImporteBase * d.Cantidad);
                var minOferta = await _context.TOfertasSubastas
                    .Where(o => o.IdRenglon == oferta.IdRenglon)
                    .MinAsync(o => (decimal?)o.Monto);
                mejorOfertaActual = minOferta ?? precioBase;
            }
            else if (oferta.IdCotizacionDetalle.HasValue)
            {
                var detalle = cotizacion.Detalles.FirstOrDefault(d => d.IdCotizacionDetalle == oferta.IdCotizacionDetalle);
                precioBase = detalle?.ImporteBase ?? 0;

                var minOferta = await _context.TOfertasSubastas
                    .Where(o => o.IdCotizacionDetalle == oferta.IdCotizacionDetalle)
                    .MinAsync(o => (decimal?)o.Monto);
                mejorOfertaActual = minOferta ?? precioBase;
            }

            // 2. Calcular Tope Máximo (Fórmula Subasta Inversa)
            decimal topeMaximoPermitido = mejorOfertaActual - (mejorOfertaActual * (margenMejora / 100m));

            if (oferta.Monto > topeMaximoPermitido)
            {
                resultItem.TextoError = $"La oferta debe ser menor o igual a ${topeMaximoPermitido:N2} (Margen del {margenMejora}%).";
            }
            else
            {
                // La oferta superó la validación
                var nuevaOferta = new TOfertaSubasta
                {
                    IdCotizacion = idCotizacion,
                    IdProveedor = idProveedor.Value,
                    IdCotizacionDetalle = oferta.IdCotizacionDetalle,
                    IdRenglon = oferta.IdRenglon,
                    Monto = oferta.Monto,
                    FechaOferta = ahora,
                    UsrIng = GetCurrentUsername(),
                    FecIng = ahora
                };
                ofertasValidas.Add(nuevaOferta);

                resultItem.Monto = oferta.Monto;
                resultItem.IdProveedor = idProveedor.Value;
                resultItem.FechaOferta = ahora;
            }

            resultados.Add(resultItem);
        }

        bool prorrogada = false;
        bool subastaCerradaPorTope = false;
        DateTime? nuevaFechaFin = cotizacion.Especificacion?.FechaFinalizacionSubasta;

        if (ofertasValidas.Any())
        {
            // KILL-SWITCH: Freno de mano por importe mínimo
            if (cotizacion.IdTipoContratacion == 7)
            {
                foreach (var ov in ofertasValidas)
                {
                    decimal? importeMinimo = null;

                    if (ov.IdRenglon.HasValue)
                    {
                        importeMinimo = cotizacion.Detalles
                            .Where(d => d.IdRenglon == ov.IdRenglon)
                            .Sum(d => (d.ImporteMinimo ?? 0) * d.Cantidad);
                    }
                    else if (ov.IdCotizacionDetalle.HasValue)
                    {
                        importeMinimo = cotizacion.Detalles
                            .FirstOrDefault(d => d.IdCotizacionDetalle == ov.IdCotizacionDetalle)?.ImporteMinimo;
                    }

                    if (importeMinimo.HasValue && importeMinimo > 0 && ov.Monto <= importeMinimo.Value)
                    {
                        subastaCerradaPorTope = true;
                        break;
                    }
                }
            }

            _context.TOfertasSubastas.AddRange(ofertasValidas);

            // 2. Aplicar Cierre Abrupto o Prórroga
            if (subastaCerradaPorTope)
            {
                cotizacion.Especificacion!.FechaFinalizacionSubasta = ahora; // Cierre inmediato
                cotizacion.IdEstado = 40; // 40 = Finalizada

                PrepareAuditableEntity(cotizacion.Especificacion, isNew: false);
                PrepareAuditableEntity(cotizacion, isNew: false);
            }
            else if (cotizacion.Especificacion!.PermiteProrroga && cotizacion.Especificacion.ProrrogaMinutos > 0)
            {
                var minutosRestantes = (cotizacion.Especificacion.FechaFinalizacionSubasta!.Value - ahora).TotalMinutes;

                if (minutosRestantes <= cotizacion.Especificacion.ProrrogaMinutos.Value)
                {
                    cotizacion.Especificacion.FechaFinalizacionSubasta = ahora.AddMinutes(cotizacion.Especificacion.ProrrogaMinutos.Value);
                    nuevaFechaFin = cotizacion.Especificacion.FechaFinalizacionSubasta;
                    prorrogada = true;

                    PrepareAuditableEntity(cotizacion.Especificacion, isNew: false);
                }
            }

            await _context.SaveChangesAsync();

            // 3. Notificaciones SignalR
            foreach (var ov in ofertasValidas)
            {
                var res = resultados.First(r => r.IdCotizacionDetalle == ov.IdCotizacionDetalle && r.IdRenglon == ov.IdRenglon);
                res.IdOfertaSubasta = ov.IdOfertaSubasta;

                await _notificationService.NotificarNuevaOfertaAsync(
                    idCotizacion, ov.IdOfertaSubasta, ov.IdCotizacionDetalle, ov.IdRenglon, ov.Monto, ov.IdProveedor, ov.FechaOferta
                );
            }

            if (subastaCerradaPorTope)
            {
                await _notificationService.NotificarCierrePorTopeAsync(idCotizacion);
            }
            else if (prorrogada && nuevaFechaFin.HasValue)
            {
                await _notificationService.NotificarProrrogaAsync(idCotizacion, nuevaFechaFin.Value);
            }
        }

        foreach (var r in resultados)
        {
            r.Prorrogada = prorrogada;
            r.FechaFinProrroga = nuevaFechaFin;
        }

        return Ok(resultados);
    }

    public async Task<OperationResponse<object>> GetHistorialAsync(int idCotizacion)
    {
        var ofertas = await _context.TOfertasSubastas
            .Where(o => o.IdCotizacion == idCotizacion)
            .OrderBy(o => o.Monto)
            .ThenBy(o => o.FechaOferta)
            .Select(o => new
            {
                o.IdOfertaSubasta,
                o.IdProveedor,
                o.IdCotizacionDetalle,
                o.IdRenglon,
                o.Monto,
                FechaOferta = o.FechaOferta.ToString("yyyy-MM-ddTHH:mm:ss")
            })
            .ToListAsync();

        return Ok<object>(ofertas);
    }
}