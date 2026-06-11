using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class CotizacionService : BaseService, ICotizacionService
{
    private readonly PortalSubastasContext _context;
    private readonly IProviderLookupService _providerLookupService;
    private readonly IPublishEndpoint _publishEndpoint;

    public CotizacionService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IProviderLookupService providerLookupService,
        IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _providerLookupService = providerLookupService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<List<CotizacionResponseDto>>> GetAllAsync(int? idVigencia)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        if (!IsSuperAdmin())
        {
            var orgId = GetUserOrganizationId();
            if (orgId.HasValue)
                query = query.Where(c => c.IdOrganizacion == orgId.Value);
        }

        var result = await query.OrderByDescending(c => c.IdCotizacion).ToListAsync();
        return Ok(_mapper.Map<List<CotizacionResponseDto>>(result));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .Include(c => c.IdUnidadAdmNavigation)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        var dto = _mapper.Map<CotizacionResponseDto>(entity);

        dto.Tipo = entity.IdTipoContratacion switch { 7 => "Subasta Inversa", 9 => "Subasta Directa", 13 => "Subasta Inversa Monto Fijo", 15 => "Subasta Inversa SEEC", _ => "Subasta" };
        dto.Estado = GetEstadoNombre(entity.IdEstado);
        dto.Modalidad = entity.Especificacion?.Redeterminacion switch { "1" => "Pública", "0" => "Privada", "2" => "Cerrada", _ => "No definida" };

        // Load Renglones separately (table may not exist yet)
        try
        {
            var renglones = await _context.TCotizacionRenglones
                .Where(r => r.IdCotizacion == id)
                .ToListAsync();
            dto.Renglones = _mapper.Map<List<CotizacionRenglonResponseDto>>(renglones);
        }
        catch { dto.Renglones = new(); }

        // Load Proveedores separately (table may not exist yet)
        try
        {
            var proveedores = await _context.TCotizacionProveedores
                .Where(p => p.IdCotizacion == id && p.FecBaja == null)
                .ToListAsync();
            dto.Proveedores = _mapper.Map<List<CotizacionProveedorResponseDto>>(proveedores);
        }
        catch { dto.Proveedores = new(); }

        // Populate NItem and NroReserva manually for each detail item
        var itemIds = dto.Detalles.Select(d => d.IdItem).ToList();
        var itemsMap = await _context.TCatalogosBiens
            .Where(i => itemIds.Contains(i.IdItem))
            .ToDictionaryAsync(i => i.IdItem, i => i.NItem);

        var resDetIds = dto.Detalles.Select(d => d.IdReservaDetalle).ToList();

        // MODIFICADO AQUÍ: Traemos NroReserva y también IdMoneda
        var resMap = await _context.TReservaDetalles
            .Include(rd => rd.IdReservaNavigation)
            .Where(rd => resDetIds.Contains(rd.IdReservaDet))
            .ToDictionaryAsync(rd => rd.IdReservaDet, rd => new {
                NroReserva = rd.IdReservaNavigation?.NroReserva,
                IdMoneda = rd.IdMoneda
            });

        foreach (var d in dto.Detalles)
        {
            if (itemsMap.TryGetValue(d.IdItem, out var nitem))
            {
                d.NItem = nitem;
            }
            if (resMap.TryGetValue(d.IdReservaDetalle, out var resData))
            {
                d.NroReserva = resData.NroReserva;
                d.IdMoneda = resData.IdMoneda; // <--- ASIGNAMOS LA MONEDA
            }
        }

        // NUEVO: Asignar la moneda al renglón (tomando la del primer ítem del grupo)
        foreach (var r in dto.Renglones)
        {
            var primerDetalle = dto.Detalles.FirstOrDefault(d => d.IdRenglon == r.IdRenglon);
            r.IdMoneda = primerDetalle?.IdMoneda;
        }

        return Ok(dto);
    }

    public async Task<OperationResponse<ReporteLicitacionResponseDto>> GetReporteLicitacionAsync(int idCotizacion)
    {
        var cotizacionResponse = await GetByIdAsync(idCotizacion);

        if (cotizacionResponse.Success != true || cotizacionResponse.Data is null)
        {
            return cotizacionResponse.Code switch
            {
                404 => NotFound<ReporteLicitacionResponseDto>(),
                400 => BadRequest<ReporteLicitacionResponseDto>(cotizacionResponse.Message ?? "No se pudo obtener la cotizacion."),
                _ => OperationResponse<ReporteLicitacionResponseDto>
                    .CustomErrorResponse(cotizacionResponse.Code ?? 500, cotizacionResponse.Message ?? "No se pudo obtener la cotizacion.")
            };
        }

        var cotizacion = cotizacionResponse.Data;

        var renglones = cotizacion.Renglones
            .OrderBy(r => r.NumeroRenglon)
            .Select(r =>
            {
                var detalles = cotizacion.Detalles
                    .Where(d => d.IdRenglon == r.IdRenglon || d.IdRenglon == r.NumeroRenglon)
                    .ToList();

                return new ReporteLicitacionRenglonResponseDto
                {
                    Numero = r.NumeroRenglon,
                    Descripcion = r.Descripcion,
                    Cantidad = detalles.Sum(d => d.Cantidad),
                    UnidadMedida = "Unidad",
                    PrecioEstimado = detalles.Sum(d => d.ImporteBase)
                };
            })
            .ToList();

        if (renglones.Count == 0 && cotizacion.Detalles.Count > 0)
        {
            renglones = cotizacion.Detalles
                .OrderBy(d => d.IdCotizacionDetalle)
                .Select((d, index) => new ReporteLicitacionRenglonResponseDto
                {
                    Numero = index + 1,
                    Descripcion = d.NItem ?? $"Item {d.IdItem}",
                    Cantidad = d.Cantidad,
                    UnidadMedida = "Unidad",
                    PrecioEstimado = d.ImporteBase
                })
                .ToList();
        }

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Licitacion {cotizacion.NroCotizacion}";

        var reporte = new ReporteLicitacionResponseDto
        {
            IdCotizacion = cotizacion.IdCotizacion,
            Numero = cotizacion.NroCotizacion,
            Titulo = titulo,
            Estado = cotizacion.Estado,
            FechaEmision = DateTimeOffset.Now,
            Renglones = renglones
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<ActaPrelacionReportResponseDto>> GetActaPrelacionAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Detalles)
            .Include(c => c.Renglones)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<ActaPrelacionReportResponseDto>();

        var criterioPorRenglon = cotizacion.Especificacion?.CriterioAdjudicacion == 1;
        var itemIds = cotizacion.Detalles.Select(d => d.IdItem).Distinct().ToList();
        var itemNames = await _context.TCatalogosBiens
            .Where(i => itemIds.Contains(i.IdItem))
            .ToDictionaryAsync(i => i.IdItem, i => i.NItem);

        var detalles = BuildActaDetalles(cotizacion, itemNames, criterioPorRenglon);
        var detallesPorItem = detalles
            .Where(d => d.IdCotizacionDetalle > 0)
            .ToDictionary(d => d.IdCotizacionDetalle);
        var detallesPorRenglon = detalles
            .Where(d => d.IdRenglon.HasValue)
            .GroupBy(d => d.IdRenglon!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var ofertas = await _context.TOfertasSubastas
            .Where(o => o.IdCotizacion == idCotizacion && o.FecBaja == null)
            .OrderBy(o => o.FechaOferta)
            .ThenBy(o => o.IdOfertaSubasta)
            .ToListAsync();

        var historial = ofertas
            .Select((oferta, index) => MapActaOferta(oferta, index + 1, false, detallesPorItem, detallesPorRenglon))
            .ToList();

        var ofertasIniciales = ofertas
            .GroupBy(o => new
            {
                o.IdProveedor,
                o.IdCotizacionDetalle,
                o.IdRenglon
            })
            .Select(g => g
                .OrderBy(o => o.FechaOferta)
                .ThenBy(o => o.IdOfertaSubasta)
                .First())
            .OrderBy(o => o.IdRenglon)
            .ThenBy(o => o.IdCotizacionDetalle)
            .ThenBy(o => o.IdProveedor)
            .Select((oferta, index) => MapActaOferta(oferta, index + 1, true, detallesPorItem, detallesPorRenglon))
            .ToList();

        var ganadores = await _context.TGanadores
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null)
            .OrderBy(g => g.IdRenglon)
            .ThenBy(g => g.IdCotizacionDetalle)
            .ThenBy(g => g.IdProveedor)
            .Select(g => new
            {
                g.IdProveedor,
                g.IdCotizacionDetalle,
                g.IdRenglon,
                g.MontoGanador,
                g.CantidadAdjudicada
            })
            .ToListAsync();

        var providerIds = ofertas.Select(o => o.IdProveedor)
            .Concat(ganadores.Select(g => g.IdProveedor))
            .Distinct()
            .ToList();
        var providers = await _providerLookupService.GetByIdsAsync(providerIds);

        foreach (var oferta in historial)
            ApplyProvider(oferta, providers);

        foreach (var oferta in ofertasIniciales)
            ApplyProvider(oferta, providers);

        var ganadoresDto = ganadores.Select(g =>
        {
            var detalle = ResolveActaDetalle(g.IdCotizacionDetalle, g.IdRenglon, detallesPorItem, detallesPorRenglon);
            providers.TryGetValue(g.IdProveedor, out var provider);

            return new ActaPrelacionGanadorResponseDto
            {
                IdProveedor = g.IdProveedor,
                Proveedor = provider?.RazonSocial ?? $"Proveedor {g.IdProveedor}",
                Cuit = provider?.Cuit,
                IdCotizacionDetalle = g.IdCotizacionDetalle,
                IdRenglon = g.IdRenglon,
                NumeroDetalle = detalle?.Numero ?? 0,
                Detalle = detalle?.Descripcion ?? "Sin detalle",
                MontoGanador = g.MontoGanador,
                CantidadAdjudicada = g.CantidadAdjudicada
            };
        }).ToList();

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new ActaPrelacionReportResponseDto
        {
            Cabecera = new ActaPrelacionCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                CriterioAdjudicacion = criterioPorRenglon ? "Renglon" : "Item",
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                MargenMejora = cotizacion.Especificacion?.MargenMejora,
                FechaInicioSubasta = cotizacion.Especificacion?.FechaInicioSubasta,
                FechaFinalizacionSubasta = cotizacion.Especificacion?.FechaFinalizacionSubasta,
                FechaEmision = DateTimeOffset.Now
            },
            Detalles = detalles,
            OfertasIniciales = ofertasIniciales,
            HistorialOfertas = historial,
            Ganadores = ganadoresDto
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<DetalleSubastaReportResponseDto>> GetDetalleSubastaAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Detalles)
            .Include(c => c.Proveedores)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<DetalleSubastaReportResponseDto>();

        var itemIds = cotizacion.Detalles.Select(d => d.IdItem).Distinct().ToList();
        var itemNames = await _context.TCatalogosBiens
            .Where(i => itemIds.Contains(i.IdItem))
            .ToDictionaryAsync(i => i.IdItem, i => i.NItem);

        var reservaDetalleIds = cotizacion.Detalles.Select(d => d.IdReservaDetalle).Distinct().ToList();
        var monedasPorReservaDetalle = await _context.TReservaDetalles
            .Where(rd => reservaDetalleIds.Contains(rd.IdReservaDet))
            .Select(rd => new
            {
                rd.IdReservaDet,
                Moneda = rd.IdMonedaNavigation != null
                    ? rd.IdMonedaNavigation.Nombre
                    : null
            })
            .ToDictionaryAsync(rd => rd.IdReservaDet, rd => rd.Moneda);

        var items = cotizacion.Detalles
            .OrderBy(d => d.IdRenglon)
            .ThenBy(d => d.IdCotizacionDetalle)
            .Select((d, index) => new DetalleSubastaItemResponseDto
            {
                IdCotizacionDetalle = d.IdCotizacionDetalle,
                IdRenglon = d.IdRenglon,
                Numero = index + 1,
                Descripcion = itemNames.TryGetValue(d.IdItem, out var nombreItem)
                    ? nombreItem
                    : $"Item {d.IdItem}",
                Cantidad = d.Cantidad,
                ImporteBase = d.ImporteBase,
                TotalBase = d.Cantidad * d.ImporteBase,
                ImporteMinimo = d.ImporteMinimo,
                Moneda = monedasPorReservaDetalle.TryGetValue(d.IdReservaDetalle, out var moneda)
                    ? moneda
                    : null
            })
            .ToList();

        var participaciones = cotizacion.Proveedores
            .Where(p => p.FecBaja == null)
            .OrderBy(p => p.IdProveedor)
            .ToList();

        var providers = await _providerLookupService.GetByIdsAsync(participaciones.Select(p => p.IdProveedor));

        var proveedores = participaciones
            .Select(p =>
            {
                providers.TryGetValue(p.IdProveedor, out var provider);

                return new DetalleSubastaProveedorResponseDto
                {
                    IdProveedor = p.IdProveedor,
                    Proveedor = provider?.RazonSocial ?? $"Proveedor {p.IdProveedor}",
                    Cuit = provider?.Cuit,
                    EstadoParticipacion = GetEstadoParticipacionProveedor(p.Ganadora)
                };
            })
            .ToList();

        var criterioPorRenglon = cotizacion.Especificacion?.CriterioAdjudicacion == 1;
        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new DetalleSubastaReportResponseDto
        {
            Cabecera = new DetalleSubastaCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                CriterioAdjudicacion = criterioPorRenglon ? "Renglon" : "Item",
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                MargenMejora = cotizacion.Especificacion?.MargenMejora,
                FechaInicioSubasta = cotizacion.Especificacion?.FechaInicioSubasta,
                FechaFinalizacionSubasta = cotizacion.Especificacion?.FechaFinalizacionSubasta,
                FechaEmision = DateTimeOffset.Now
            },
            Items = items,
            Proveedores = proveedores
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<ProveedoresInvitadosReportResponseDto>> GetProveedoresInvitadosAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Proveedores)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<ProveedoresInvitadosReportResponseDto>();

        var participaciones = cotizacion.Proveedores
            .Where(p => p.FecBaja == null)
            .GroupBy(p => p.IdProveedor)
            .Select(g => g.First())
            .OrderBy(p => p.IdProveedor)
            .ToList();

        var providers = await _providerLookupService.GetByIdsAsync(participaciones.Select(p => p.IdProveedor));

        var proveedores = participaciones
            .Select(p =>
            {
                providers.TryGetValue(p.IdProveedor, out var provider);
                var mail = !string.IsNullOrWhiteSpace(provider?.EmailInstitucional)
                    ? provider.EmailInstitucional
                    : provider?.EmailAlternativo;

                return new ProveedorInvitadoResponseDto
                {
                    IdProveedor = p.IdProveedor,
                    RazonSocial = provider?.RazonSocial ?? $"Proveedor {p.IdProveedor}",
                    Cuit = provider?.Cuit,
                    Mail = mail,
                    Telefono = null
                };
            })
            .OrderBy(p => p.RazonSocial)
            .ToList();

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new ProveedoresInvitadosReportResponseDto
        {
            Cabecera = new ProveedoresInvitadosCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                FechaEmision = DateTimeOffset.Now
            },
            Proveedores = proveedores
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<PreguntasRespuestasReportResponseDto>> GetPreguntasRespuestasAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<PreguntasRespuestasReportResponseDto>();

        var consultas = await _context.TMensajes
            .Where(m => m.IdCotizacion == idCotizacion)
            .OrderBy(m => m.FecIng)
            .ThenBy(m => m.IdMensaje)
            .Select(m => new PreguntaRespuestaResponseDto
            {
                IdMensaje = m.IdMensaje,
                IdProveedor = m.IdProveedor,
                UsuarioPregunta = m.Usuario,
                Pregunta = m.Contenido,
                FechaPregunta = m.FecIng,
                Respuesta = m.Respuesta,
                UsuarioRespuesta = m.UsuarioRespuesta,
                FechaRespuesta = m.FechaRespuesta
            })
            .ToListAsync();

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new PreguntasRespuestasReportResponseDto
        {
            Cabecera = new PreguntasRespuestasCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                FechaEmision = DateTimeOffset.Now
            },
            Consultas = consultas
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<DesistimientoReportResponseDto>> GetDesistimientoAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<DesistimientoReportResponseDto>();

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new DesistimientoReportResponseDto
        {
            Cabecera = new DesistimientoCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                FechaEmision = DateTimeOffset.Now
            },
            Observaciones = cotizacion.Observacion,
            UsuarioDesistimiento = cotizacion.UsrMod ?? cotizacion.UsrBaja,
            FechaDesistimiento = cotizacion.FecMod ?? cotizacion.FecBaja
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<ObservacionesProveedoresReportResponseDto>> GetObservacionesProveedoresAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Proveedores)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<ObservacionesProveedoresReportResponseDto>();

        var observaciones = new List<ObservacionProveedorResponseDto>();

        if (!string.IsNullOrWhiteSpace(cotizacion.Observacion))
        {
            observaciones.Add(new ObservacionProveedorResponseDto
            {
                IdProveedor = null,
                Proveedor = "Observacion general de la cotizacion",
                Cuit = null,
                Observacion = cotizacion.Observacion,
                Origen = "Cotizacion"
            });
        }

        var providerIds = cotizacion.Proveedores
            .Where(p => p.FecBaja == null)
            .Select(p => p.IdProveedor)
            .Distinct()
            .ToList();

        var providers = await _providerLookupService.GetByIdsAsync(providerIds);

        foreach (var proveedor in cotizacion.Proveedores.Where(p => p.FecBaja == null).OrderBy(p => p.IdProveedor))
        {
            providers.TryGetValue(proveedor.IdProveedor, out var provider);

            if (proveedor.Ganadora == "D")
            {
                observaciones.Add(new ObservacionProveedorResponseDto
                {
                    IdProveedor = proveedor.IdProveedor,
                    Proveedor = provider?.RazonSocial ?? $"Proveedor {proveedor.IdProveedor}",
                    Cuit = provider?.Cuit,
                    Observacion = "Proveedor descartado en la cotizacion.",
                    Origen = "Estado de participacion"
                });
            }
        }

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new ObservacionesProveedoresReportResponseDto
        {
            Cabecera = new ObservacionesProveedoresCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                FechaLimiteImpugnar = cotizacion.Especificacion?.FechaLimiteImpugnar,
                FechaEmision = DateTimeOffset.Now
            },
            Observaciones = observaciones
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<AuditoriaSubastaReportResponseDto>> GetAuditoriaSubastaAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<AuditoriaSubastaReportResponseDto>();

        var cantidadDocumentos = await _context.TCotizacionDocumentos
            .CountAsync(d => d.IdCotizacion == idCotizacion && d.FecBaja == null);

        var cantidadOfertas = await _context.TOfertasSubastas
            .CountAsync(o => o.IdCotizacion == idCotizacion && o.FecBaja == null);

        var cantidadProveedores = await _context.TCotizacionProveedores
            .Where(p => p.IdCotizacion == idCotizacion && p.FecBaja == null)
            .Select(p => p.IdProveedor)
            .Distinct()
            .CountAsync();

        var primerDocumento = await _context.TCotizacionDocumentos
            .Where(d => d.IdCotizacion == idCotizacion && d.FecBaja == null)
            .OrderBy(d => d.FecIng)
            .Select(d => new { d.FecIng, d.UsrIng })
            .FirstOrDefaultAsync();

        var primeraOferta = await _context.TOfertasSubastas
            .Where(o => o.IdCotizacion == idCotizacion && o.FecBaja == null)
            .OrderBy(o => o.FechaOferta)
            .Select(o => new { Fecha = (DateTime?)o.FechaOferta, o.UsrIng })
            .FirstOrDefaultAsync();

        var primerProveedor = await _context.TCotizacionProveedores
            .Where(p => p.IdCotizacion == idCotizacion && p.FecBaja == null)
            .OrderBy(p => p.FecIng)
            .Select(p => new { p.FecIng, p.UsrIng })
            .FirstOrDefaultAsync();

        var movimientos = new List<AuditoriaSubastaMovimientoResponseDto>
        {
            new()
            {
                Tipo = "Cotizacion",
                Descripcion = "Alta de la cotizacion/subasta.",
                Cantidad = 1,
                Usuario = cotizacion.UsrIng,
                Fecha = cotizacion.FecIng
            },
            new()
            {
                Tipo = "Documentos",
                Descripcion = "Documentos adjuntos registrados para la subasta.",
                Cantidad = cantidadDocumentos,
                Usuario = primerDocumento?.UsrIng,
                Fecha = primerDocumento?.FecIng
            },
            new()
            {
                Tipo = "Ofertas",
                Descripcion = "Ofertas registradas para la subasta.",
                Cantidad = cantidadOfertas,
                Usuario = primeraOferta?.UsrIng,
                Fecha = primeraOferta?.Fecha
            },
            new()
            {
                Tipo = "Proveedores",
                Descripcion = "Proveedores asociados/invitados a la subasta.",
                Cantidad = cantidadProveedores,
                Usuario = primerProveedor?.UsrIng,
                Fecha = primerProveedor?.FecIng
            }
        };

        if (cotizacion.FecMod.HasValue || !string.IsNullOrWhiteSpace(cotizacion.UsrMod))
        {
            movimientos.Add(new AuditoriaSubastaMovimientoResponseDto
            {
                Tipo = "Estado",
                Descripcion = $"Ultima modificacion registrada. Estado actual: {GetEstadoNombre(cotizacion.IdEstado)}.",
                Cantidad = 1,
                Usuario = cotizacion.UsrMod,
                Fecha = cotizacion.FecMod
            });
        }

        var criterioPorRenglon = cotizacion.Especificacion?.CriterioAdjudicacion == 1;
        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new AuditoriaSubastaReportResponseDto
        {
            Cabecera = new AuditoriaSubastaCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                CriterioAdjudicacion = criterioPorRenglon ? "Renglon" : "Item",
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                FechaCotizacion = cotizacion.FecIng,
                FechaInicioSubasta = cotizacion.Especificacion?.FechaInicioSubasta,
                FechaEmision = DateTimeOffset.Now
            },
            Resumen = new AuditoriaSubastaResumenResponseDto
            {
                CantidadDocumentos = cantidadDocumentos,
                CantidadOfertas = cantidadOfertas,
                CantidadProveedores = cantidadProveedores
            },
            Movimientos = movimientos
        };

        return Ok(reporte);
    }

    public async Task<OperationResponse<CotizacionResponseDto>> CreateAsync(CotizacionRequestDto dto)
    {
        var entity = _mapper.Map<TCotizacion>(dto);
        
        // Logica legacy de inicialización
        entity.IdEstado = 4; // Generado
        entity.IdOrganizacion = GetUserOrganizationId() ?? dto.IdOrganizacion;
        if (entity.IdOrganizacion == 0) entity.IdOrganizacion = 1; // fallback
        
        // Auto-numérico NRO_COTIZACION (simplificado para MVP, se debería hacer transaccional)
        var vigencia = await _context.TVigencias.FindAsync(dto.IdVigencia);
        var maxCotizacion = await _context.TCotizaciones
            .Where(c => c.IdVigencia == dto.IdVigencia && c.IdUnidadAdm == dto.IdUnidadAdm)
            .Select(c => c.NroCotizacion)
            .ToListAsync();
            
        int nextId = 1;
        if (maxCotizacion.Any())
        {
            var maxVal = maxCotizacion.Select(x => {
                var parts = x.Split('/');
                return parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0;
            }).Max();
            nextId = maxVal + 1;
        }

        entity.NroCotizacion = $"{vigencia?.Ejercicio ?? DateTime.Now.Year}/{nextId.ToString().PadLeft(6, '0')}";

        PrepareAuditableEntity(entity, isNew: true);
        
        if (entity.Especificacion != null)
            PrepareAuditableEntity(entity.Especificacion, isNew: true);

        foreach(var det in entity.Detalles)
            PrepareAuditableEntity(det, isNew: true);

        foreach(var ren in entity.Renglones)
            PrepareAuditableEntity(ren, isNew: true);

        // Link detalle items to renglones using NumeroRenglon from the request.
        // The frontend sends ren.id as both numeroRenglon and idRenglon,
        // but the real DB id_renglon is auto-generated. Match by NumeroRenglon.
        if (entity.Renglones.Any())
        {
            var renglonByNro = entity.Renglones.ToDictionary(r => r.NumeroRenglon);
            foreach (var det in entity.Detalles.Where(d => d.IdRenglon.HasValue))
            {
                var nroRenglon = det.IdRenglon.Value;
                if (renglonByNro.TryGetValue(nroRenglon, out var ren))
                {
                    det.IdRenglonNavigation = ren;
                    det.IdRenglon = null; // EF Core will resolve from navigation
                }
            }
        }

        _context.TCotizaciones.Add(entity);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_CREADA", "LICITACIONES", new { entity.NroCotizacion, entity.IdCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> UpdateAsync(int id, CotizacionRequestDto dto)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        // Logica basica actualización
        _mapper.Map(dto, entity);
        PrepareAuditableEntity(entity, isNew: false);
        
        if (entity.Especificacion != null)
            PrepareAuditableEntity(entity.Especificacion, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_MODIFICADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TCotizaciones.FindAsync(id);
        if (entity == null) return NotFound<bool>();

        if (entity.IdEstado != 4)
            return BadRequest<bool>("Solo se puede anular una subasta en estado Generado.");

        entity.IdEstado = 20; // Anulado (baja lógica según legacy)
        PrepareAuditableEntity(entity, isNew: false, isDeleted: true);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_ANULADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        return Ok(true);
    }

    public async Task<OperationResponse<CotizacionResponseDto>> NotificarAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        if (entity.IdEstado != 4)
            return BadRequest<CotizacionResponseDto>("Solo se puede publicar una subasta en estado Generado.");

        entity.IdEstado = 39; // EnviadaPendiente (publicada)
        PrepareAuditableEntity(entity, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_PUBLICADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> FinalizarAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        if (entity.IdEstado != 39)
            return BadRequest<CotizacionResponseDto>("Solo se puede finalizar una subasta en estado EnviadaPendiente.");

        entity.IdEstado = 40; // Finalizada
        PrepareAuditableEntity(entity, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_FINALIZADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> DesistirAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        if (entity.IdEstado != 39 && entity.IdEstado != 40)
            return BadRequest<CotizacionResponseDto>("Solo se puede desistir en estado EnviadaPendiente o Finalizada.");

        entity.IdEstado = 47; // Desistir
        PrepareAuditableEntity(entity, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_DESISTIDA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    // --- Endpoints Dashboard ---
    private IQueryable<TCotizacion> GetDashboardBaseQuery(int? idVigencia)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        return query;
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasEnCursoAsync(int? idVigencia)
    {
        var now = DateTime.Now;
        var query = GetDashboardBaseQuery(idVigencia)
            .Where(c => c.IdEstado == 39 // EnvPend
                     && c.Especificacion.FechaInicioSubasta <= now 
                     && c.Especificacion.FechaFinalizacionSubasta >= now);

        var data = await query.ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasProximasAsync(int? idVigencia)
    {
        var now = DateTime.Now;
        var query = GetDashboardBaseQuery(idVigencia)
            .Where(c => c.IdEstado == 39 // EnvPend
                     && c.Especificacion.FechaInicioSubasta > now)
            .OrderBy(c => c.Especificacion.FechaInicioSubasta)
            .Take(6);

        var data = await query.ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasDelMesAsync(int? idVigencia)
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var query = GetDashboardBaseQuery(idVigencia)
            .Where(c => c.Especificacion.FechaInicioSubasta >= startOfMonth 
                     && c.Especificacion.FechaInicioSubasta <= endOfMonth)
            .OrderBy(c => c.Especificacion.FechaInicioSubasta);

        var data = await query.ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> BuscarAsync(
        int? idVigencia, int? idEstado, int? idTipoContratacion,
        string? nro, string? expte, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .AsQueryable();

        // Filtros básicos
        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        if (idEstado.HasValue)
            query = query.Where(c => c.IdEstado == idEstado.Value);

        if (idTipoContratacion.HasValue)
            query = query.Where(c => c.IdTipoContratacion == idTipoContratacion.Value);

        if (!string.IsNullOrWhiteSpace(nro))
            query = query.Where(c => c.NroCotizacion.Contains(nro));

        if (!string.IsNullOrWhiteSpace(expte))
            query = query.Where(c => c.Especificacion.NroExpediente.Contains(expte) || c.Observacion.Contains(expte));

        if (fechaDesde.HasValue)
            query = query.Where(c => c.Especificacion.FechaInicioSubasta >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(c => c.Especificacion.FechaFinalizacionSubasta <= fechaHasta.Value);

        // Extraer contexto del usuario desde el JWT
        var esAdmin = IsSuperAdmin();
        var proveedorId = GetUserProveedorId();

        // Lógica de visibilidad (replica del sistema viejo)
        // Admin ve todo. Proveedor solo ve:
        // - Subastas Públicas (Redeterminacion = "1")
        // - Subastas Privadas (Redeterminacion = "0") donde está invitado
        // - NO ve Cerradas (Redeterminacion = "2") a menos que esté invitado
        if (!esAdmin && proveedorId.HasValue)
        {
            query = query.Where(c =>
                c.Especificacion.Redeterminacion == "1" // Pública: todos ven
                || (c.Especificacion.Redeterminacion == "0" // Privada: solo invitados
                    && c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.FecBaja == null))
                || (c.Especificacion.Redeterminacion == "2" // Cerrada: solo invitados
                    && c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.FecBaja == null))
            );
        }

        var data = await query.OrderByDescending(c => c.IdCotizacion).Take(100).ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> ProrrogarAsync(int id, int minutos)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null || entity.Especificacion == null) return NotFound<CotizacionResponseDto>();

        entity.Especificacion.FechaFinalizacionSubasta = entity.Especificacion.FechaFinalizacionSubasta?.AddMinutes(minutos);
        PrepareAuditableEntity(entity.Especificacion, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_PRORROGADA", "LICITACIONES", new { entity.IdCotizacion, Minutos = minutos, NuevaFechaFin = entity.Especificacion?.FechaFinalizacionSubasta });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DesistirParticipacionAsync(int idCotizacion)
    {
        var idProveedor = GetUserProveedorId();
        if (!idProveedor.HasValue)
            return BadRequest<bool>("No se pudo identificar al proveedor activo.");

        var participacion = await _context.TCotizacionProveedores
            .FirstOrDefaultAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == idProveedor.Value && p.FecBaja == null);

        if (participacion == null)
            return BadRequest<bool>("No estás asignado como proveedor a esta subasta.");

        participacion.Ganadora = "D";
        PrepareAuditableEntity(participacion, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "PROVEEDOR_DESISTE_SUBASTA", "LICITACIONES", new { IdCotizacion = idCotizacion, IdProveedor = idProveedor });

        return Ok(true);
    }



    public async Task<OperationResponse<VerificacionDocumentacionReportResponseDto>> GetVerificacionDocumentacionAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion is null)
            return NotFound<VerificacionDocumentacionReportResponseDto>();

        var documentosCotizacion = await _context.TCotizacionDocumentos
            .Where(d => d.IdCotizacion == idCotizacion && d.FecBaja == null)
            .OrderBy(d => d.FecIng)
            .Select(d => new
            {
                d.TipoDocumento,
                d.NombreArchivo,
                d.UrlArchivo,
                d.FecIng
            })
            .ToListAsync();

        var garantias = await _context.TGarantiasSubastas
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null)
            .OrderBy(g => g.IdProveedor)
            .ThenBy(g => g.FecIng)
            .Select(g => new
            {
                g.IdGarantia,
                g.IdProveedor,
                g.IdTipoDocumento,
                g.CompaniaAseguradora,
                g.NroPoliza,
                g.MontoCaucion,
                g.MontoPagare,
                g.FechaPagare,
                g.Observacion,
                g.NombreArchivo,
                g.UrlArchivo,
                g.FecIng
            })
            .ToListAsync();

        var providerIds = garantias
            .Select(g => g.IdProveedor)
            .Distinct()
            .ToList();

        var providers = await _providerLookupService.GetByIdsAsync(providerIds);
        var documentos = new List<VerificacionDocumentacionItemResponseDto>();

        documentos.AddRange(documentosCotizacion.Select(d => new VerificacionDocumentacionItemResponseDto
        {
            Origen = "Cotizacion",
            IdProveedor = null,
            Proveedor = "Organismo / Cotizacion",
            Cuit = null,
            TipoDocumento = string.IsNullOrWhiteSpace(d.TipoDocumento) ? "Documento de cotizacion" : d.TipoDocumento,
            NombreArchivo = d.NombreArchivo,
            UrlArchivo = d.UrlArchivo,
            Estado = "Presentado",
            FechaPresentacion = d.FecIng,
            Observaciones = "Documento general asociado a la subasta."
        }));

        foreach (var garantia in garantias)
        {
            providers.TryGetValue(garantia.IdProveedor, out var provider);

            documentos.Add(new VerificacionDocumentacionItemResponseDto
            {
                Origen = "Proveedor",
                IdProveedor = garantia.IdProveedor,
                Proveedor = provider?.RazonSocial ?? $"Proveedor {garantia.IdProveedor}",
                Cuit = provider?.Cuit,
                TipoDocumento = ResolveTipoDocumentoGarantia(
                    garantia.IdTipoDocumento,
                    garantia.NroPoliza,
                    garantia.MontoCaucion,
                    garantia.MontoPagare,
                    garantia.FechaPagare),
                NombreArchivo = garantia.NombreArchivo,
                UrlArchivo = garantia.UrlArchivo,
                Estado = "Presentado",
                FechaPresentacion = garantia.FecIng,
                Observaciones = BuildObservacionGarantia(
                    garantia.CompaniaAseguradora,
                    garantia.NroPoliza,
                    garantia.MontoCaucion,
                    garantia.MontoPagare,
                    garantia.FechaPagare,
                    garantia.Observacion)
            });
        }

        var titulo = !string.IsNullOrWhiteSpace(cotizacion.Observacion)
            ? cotizacion.Observacion
            : $"Subasta {cotizacion.NroCotizacion}";

        var reporte = new VerificacionDocumentacionReportResponseDto
        {
            Cabecera = new VerificacionDocumentacionCabeceraResponseDto
            {
                IdCotizacion = cotizacion.IdCotizacion,
                NumeroCotizacion = cotizacion.NroCotizacion,
                Titulo = titulo,
                Estado = GetEstadoNombre(cotizacion.IdEstado),
                TipoContratacion = GetTipoContratacionNombre(cotizacion.IdTipoContratacion),
                CriterioAdjudicacion = cotizacion.Especificacion?.CriterioAdjudicacion == 1 ? "Renglon" : "Item",
                UnidadAdministrativa = cotizacion.IdUnidadAdmNavigation?.NombreUnidadAdm ?? string.Empty,
                NumeroExpediente = cotizacion.Especificacion?.NroExpediente,
                FechaInicioSubasta = cotizacion.Especificacion?.FechaInicioSubasta,
                FechaFinalizacionSubasta = cotizacion.Especificacion?.FechaFinalizacionSubasta,
                FechaEmision = DateTimeOffset.UtcNow,
                NotaAdecuacion = "Reporte 9 readecuado: el legacy leia T_DOC_ADJ_LICITACION.VERIFICADO; el modelo nuevo no expone estado de verificacion/rechazo por documento, por eso se informan documentos y garantias presentadas sin inventar estado legacy."
            },
            Documentos = documentos
                .OrderBy(d => d.Origen)
                .ThenBy(d => d.Proveedor)
                .ThenBy(d => d.FechaPresentacion)
                .ToList()
        };

        return Ok(reporte);
    }

    private static void ApplyProvider(
        ActaPrelacionOfertaResponseDto oferta,
        IReadOnlyDictionary<int, ProviderReportLookupDto> providers)
    {
        if (!providers.TryGetValue(oferta.IdProveedor, out var provider))
            return;

        oferta.Proveedor = provider.RazonSocial;
        oferta.Cuit = provider.Cuit;
    }

    private static List<ActaPrelacionDetalleResponseDto> BuildActaDetalles(
        TCotizacion cotizacion,
        IReadOnlyDictionary<int, string> itemNames,
        bool criterioPorRenglon)
    {
        if (criterioPorRenglon && cotizacion.Renglones.Any())
        {
            return cotizacion.Renglones
                .OrderBy(r => r.NumeroRenglon)
                .Select(r =>
                {
                    var detalles = cotizacion.Detalles.Where(d => d.IdRenglon == r.IdRenglon).ToList();

                    return new ActaPrelacionDetalleResponseDto
                    {
                        IdCotizacionDetalle = 0,
                        IdRenglon = r.IdRenglon,
                        Numero = r.NumeroRenglon,
                        Descripcion = r.Descripcion ?? $"Renglon {r.NumeroRenglon}",
                        Cantidad = detalles.Sum(d => d.Cantidad),
                        PrecioBase = detalles.Sum(d => d.ImporteBase),
                        TotalBase = detalles.Sum(d => d.ImporteBase * d.Cantidad)
                    };
                })
                .ToList();
        }

        return cotizacion.Detalles
            .OrderBy(d => d.IdCotizacionDetalle)
            .Select((d, index) => new ActaPrelacionDetalleResponseDto
            {
                IdCotizacionDetalle = d.IdCotizacionDetalle,
                IdRenglon = d.IdRenglon,
                Numero = index + 1,
                Descripcion = itemNames.TryGetValue(d.IdItem, out var nombreItem)
                    ? nombreItem
                    : $"Item {d.IdItem}",
                Cantidad = d.Cantidad,
                PrecioBase = d.ImporteBase,
                TotalBase = d.ImporteBase * d.Cantidad
            })
            .ToList();
    }

    private static ActaPrelacionOfertaResponseDto MapActaOferta(
        TOfertaSubasta oferta,
        int numeroOferta,
        bool esOfertaInicial,
        IReadOnlyDictionary<int, ActaPrelacionDetalleResponseDto> detallesPorItem,
        IReadOnlyDictionary<int, ActaPrelacionDetalleResponseDto> detallesPorRenglon)
    {
        var detalle = ResolveActaDetalle(
            oferta.IdCotizacionDetalle,
            oferta.IdRenglon,
            detallesPorItem,
            detallesPorRenglon);

        var cantidad = detalle?.Cantidad ?? 1;
        var esOfertaPorRenglon = oferta.IdRenglon.HasValue;

        return new ActaPrelacionOfertaResponseDto
        {
            IdOfertaSubasta = oferta.IdOfertaSubasta,
            IdProveedor = oferta.IdProveedor,
            Proveedor = $"Proveedor {oferta.IdProveedor}",
            Cuit = null,
            IdCotizacionDetalle = oferta.IdCotizacionDetalle,
            IdRenglon = oferta.IdRenglon,
            NumeroDetalle = detalle?.Numero ?? 0,
            Detalle = detalle?.Descripcion ?? "Sin detalle",
            Cantidad = cantidad,
            Monto = oferta.Monto,
            Total = esOfertaPorRenglon ? oferta.Monto : oferta.Monto * cantidad,
            FechaOferta = oferta.FechaOferta,
            NumeroOferta = numeroOferta,
            EsOfertaInicial = esOfertaInicial
        };
    }

    private static ActaPrelacionDetalleResponseDto? ResolveActaDetalle(
        int? idCotizacionDetalle,
        int? idRenglon,
        IReadOnlyDictionary<int, ActaPrelacionDetalleResponseDto> detallesPorItem,
        IReadOnlyDictionary<int, ActaPrelacionDetalleResponseDto> detallesPorRenglon)
    {
        if (idRenglon.HasValue && detallesPorRenglon.TryGetValue(idRenglon.Value, out var renglon))
            return renglon;

        if (idCotizacionDetalle.HasValue && detallesPorItem.TryGetValue(idCotizacionDetalle.Value, out var item))
            return item;

        return null;
    }


    private static string ResolveTipoDocumentoGarantia(
        int idTipoDocumento,
        string? nroPoliza,
        decimal? montoCaucion,
        decimal? montoPagare,
        DateOnly? fechaPagare)
    {
        if (!string.IsNullOrWhiteSpace(nroPoliza) || montoCaucion.HasValue)
            return "Garantia de caucion";

        if (montoPagare.HasValue || fechaPagare.HasValue)
            return "Pagare";

        return idTipoDocumento > 0
            ? $"Documento proveedor tipo {idTipoDocumento}"
            : "Documento proveedor";
    }

    private static string? BuildObservacionGarantia(
        string? companiaAseguradora,
        string? nroPoliza,
        decimal? montoCaucion,
        decimal? montoPagare,
        DateOnly? fechaPagare,
        string? observacion)
    {
        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(companiaAseguradora))
            partes.Add($"Compania aseguradora: {companiaAseguradora}");

        if (!string.IsNullOrWhiteSpace(nroPoliza))
            partes.Add($"Poliza: {nroPoliza}");

        if (montoCaucion.HasValue)
            partes.Add($"Monto caucion: {montoCaucion.Value:N2}");

        if (montoPagare.HasValue)
            partes.Add($"Monto pagare: {montoPagare.Value:N2}");

        if (fechaPagare.HasValue)
            partes.Add($"Fecha pagare: {fechaPagare.Value:dd/MM/yyyy}");

        if (!string.IsNullOrWhiteSpace(observacion))
            partes.Add(observacion);

        return partes.Count == 0 ? null : string.Join(" | ", partes);
    }

public async Task<OperationResponse<MetricasAhorroDto>> GetMetricasAhorroAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion == null) return NotFound<MetricasAhorroDto>();

        bool isRenglon = cotizacion.Especificacion?.CriterioAdjudicacion == 1;
        decimal presupuestoBaseTotal = 0;
        decimal mejorOfertaTotal = 0;

        if (isRenglon)
        {
            var renglonesIds = cotizacion.Detalles.Where(d => d.IdRenglon.HasValue).Select(d => d.IdRenglon.Value).Distinct().ToList();

            foreach (var idRenglon in renglonesIds)
            {
                decimal baseRenglon = cotizacion.Detalles.Where(d => d.IdRenglon == idRenglon).Sum(d => d.ImporteBase * d.Cantidad);
                presupuestoBaseTotal += baseRenglon;
                
                var minOferta = await _context.TOfertasSubastas
                    .Where(o => o.IdRenglon == idRenglon && o.FecBaja == null)
                    .MinAsync(o => (decimal?)o.Monto);

                mejorOfertaTotal += minOferta ?? baseRenglon;
            }
        }
        else
        {
            foreach (var detalle in cotizacion.Detalles)
            {
                decimal baseItem = detalle.ImporteBase * detalle.Cantidad;
                presupuestoBaseTotal += baseItem;

                var minOferta = await _context.TOfertasSubastas
                    .Where(o => o.IdCotizacionDetalle == detalle.IdCotizacionDetalle && o.FecBaja == null)
                    .MinAsync(o => (decimal?)o.Monto);

                mejorOfertaTotal += (minOferta ?? detalle.ImporteBase) * detalle.Cantidad;
            }
        }

        decimal ahorroPorcentaje = 0;
        if (presupuestoBaseTotal > 0)
        {
            ahorroPorcentaje = ((presupuestoBaseTotal - mejorOfertaTotal) / presupuestoBaseTotal) * 100m;
        }

        return Ok(new MetricasAhorroDto
        {
            PresupuestoBase = Math.Round(presupuestoBaseTotal, 2),
            MejorOfertaFinal = Math.Round(mejorOfertaTotal, 2),
            AhorroPorcentaje = Math.Round(ahorroPorcentaje, 2)
        });

    }

    private string GetEstadoNombre(int idEstado) => idEstado switch
    {
        4 => "Generado",
        20 => "Anulado",
        39 => "Enviada Pendiente",
        40 => "Finalizada",
        47 => "Desistida",
        _ => "Desconocido"
    };

    private string GetTipoContratacionNombre(int idTipo) => idTipo switch
    {
        7 => "Subasta Electrónica Inversa",
        9 => "Subasta Electrónica Directa",
        13 => "Subasta Inversa Monto Fijo",
        15 => "Subasta Inversa SEEC",
        _ => "Subasta"
    };

    private static string GetEstadoParticipacionProveedor(string? ganadora) => ganadora switch
    {
        "E" => "Ganador",
        "D" => "Descartado",
        "N" => "Participante",
        _ => "Participante"
    };


    private List<SubastaDashboardDto> MapToDashboard(List<TCotizacion> data)
    {
        return data.Select(c => new SubastaDashboardDto
        {
            IdCotizacion = c.IdCotizacion,
            IdEstado = c.IdEstado,
            NroCotizacion = c.NroCotizacion,
            IdTipoContratacion = c.IdTipoContratacion,
            TipoContratacion = GetTipoContratacionNombre(c.IdTipoContratacion),
            Tipo = c.IdTipoContratacion == 7 ? "Subasta Inversa" : "Subasta Directa",
            Estado = GetEstadoNombre(c.IdEstado),
            Titulo = c.Observacion ?? "Subasta " + c.NroCotizacion,
            UnidadAdm = c.IdUnidadAdmNavigation?.NombreUnidadAdm ?? "",
            ObjetoContratacion = c.Observacion ?? "",
            CriterioAdjudicacion = c.Especificacion?.CriterioAdjudicacion,
            Redeterminacion = c.Especificacion?.Redeterminacion,
            FechaInicio = c.Especificacion?.FechaInicioSubasta,
            FechaFin = c.Especificacion?.FechaFinalizacionSubasta,
            FechaConsulta = c.Especificacion?.FechaLimiteConsultas,
            FechaFinSubasta = c.Especificacion?.FechaFinalizacionSubasta,
            VerInformeFinal = c.IdEstado == 40, // Finalizada

            MostrarBotonMejora = c.Especificacion?.MostrarBotonMejora ?? false,
            TipoSobre = c.Especificacion?.TipoSobre,
            FechaLimiteImpugnar = c.Especificacion?.FechaLimiteImpugnar,
            FechaAperturaSobreUno = c.Especificacion?.FechaAperturaSobreUno,
            FechaAperturaSobreDos = c.Especificacion?.FechaAperturaSobreDos
        }).ToList();
    }
}
