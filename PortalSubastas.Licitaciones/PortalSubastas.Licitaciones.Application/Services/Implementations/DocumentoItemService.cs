using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class DocumentoItemService : BaseService, IDocumentoItemService
{
    private new readonly PortalSubastasContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IConfiguration _configuration;

    public DocumentoItemService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IFileStorageService fileStorageService,
        IConfiguration configuration)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _configuration = configuration;
    }

    public async Task<OperationResponse<List<DocumentoItemResponseDto>>> GetByItemAsync(int idCotizacion, int? idCotizacionDetalle, int? idRenglon)
    {
        // El admin ve todo, el proveedor solo ve lo suyo.
        var idProveedor = GetUserProveedorId();
        bool isAdmin = IsSuperAdmin();

        var query = _context.TDocumentoItemProveedores
            .Where(d => d.IdCotizacion == idCotizacion && d.FecBaja == null);

        if (idCotizacionDetalle.HasValue)
            query = query.Where(d => d.IdCotizacionDetalle == idCotizacionDetalle.Value);
        else if (idRenglon.HasValue)
            query = query.Where(d => d.IdRenglon == idRenglon.Value);

        if (!isAdmin && idProveedor.HasValue)
            query = query.Where(d => d.IdProveedor == idProveedor.Value);

        var list = await query.OrderByDescending(d => d.FecIng).ToListAsync();
        return Ok(_mapper.Map<List<DocumentoItemResponseDto>>(list));
    }

    public async Task<OperationResponse<DocumentoItemResponseDto>> UploadAsync(DocumentoItemRequestDto request)
    {
        var idProveedor = GetUserProveedorId();
        if (!idProveedor.HasValue) return BadRequest<DocumentoItemResponseDto>("Proveedor no identificado.");

        if (request.Archivo == null || request.Archivo.Length == 0)
            return BadRequest<DocumentoItemResponseDto>("El archivo es obligatorio.");

        if (request.Archivo.Length > 20 * 1024 * 1024)
            return BadRequest<DocumentoItemResponseDto>("El archivo no puede superar los 20 MB.");

        // Verificamos que no se haya "Enviado de forma definitiva" para este ítem
        var yaEnviado = await _context.TDocumentoItemProveedores.AnyAsync(d =>
            d.IdCotizacion == request.IdCotizacion &&
            d.IdProveedor == idProveedor.Value &&
            (request.IdRenglon.HasValue ? d.IdRenglon == request.IdRenglon : d.IdCotizacionDetalle == request.IdCotizacionDetalle) &&
            d.Enviado == true &&
            d.FecBaja == null);

        if (yaEnviado)
            return BadRequest<DocumentoItemResponseDto>("La documentación ya fue enviada definitivamente y no admite modificaciones.");

        string bucketName = _configuration["CloudflareR2:BucketName"] ?? "subasta-electronica";
        string urlArchivo = await _fileStorageService.UploadFileAsync(request.Archivo, bucketName, "documentacion-item-renglon/");

        var entity = new TDocumentoItemProveedor
        {
            IdCotizacion = request.IdCotizacion,
            IdCotizacionDetalle = request.IdCotizacionDetalle,
            IdRenglon = request.IdRenglon,
            IdProveedor = idProveedor.Value,
            NombreArchivo = request.Archivo.FileName,
            UrlArchivo = urlArchivo,
            Enviado = false
        };

        PrepareAuditableEntity(entity, isNew: true);
        _context.TDocumentoItemProveedores.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<DocumentoItemResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int idDocItem)
    {
        var entity = await _context.TDocumentoItemProveedores.FindAsync(idDocItem);
        if (entity == null) return NotFound<bool>();

        var idProveedor = GetUserProveedorId();
        bool isAdmin = IsSuperAdmin();

        if (!isAdmin && entity.IdProveedor != idProveedor)
            return Unauthorized<bool>("No puedes eliminar documentos de otro proveedor.");

        if (entity.Enviado)
            return BadRequest<bool>("No se puede eliminar un documento que ya fue enviado definitivamente.");

        return await DeleteAsync(entity, _context);
    }

    public async Task<OperationResponse<bool>> EnviarDocumentacionDefinitivaAsync(int idCotizacion, int? idCotizacionDetalle, int? idRenglon)
    {
        var idProveedor = GetUserProveedorId();
        if (!idProveedor.HasValue) return BadRequest<bool>("Proveedor no identificado.");

        var query = _context.TDocumentoItemProveedores
            .Where(d => d.IdCotizacion == idCotizacion && d.IdProveedor == idProveedor.Value && d.Enviado == false && d.FecBaja == null);

        if (idCotizacionDetalle.HasValue)
            query = query.Where(d => d.IdCotizacionDetalle == idCotizacionDetalle.Value);
        else if (idRenglon.HasValue)
            query = query.Where(d => d.IdRenglon == idRenglon.Value);

        var documentos = await query.ToListAsync();

        if (!documentos.Any())
            return BadRequest<bool>("No hay documentos pendientes de envío para este ítem/renglón.");

        foreach (var doc in documentos)
        {
            doc.Enviado = true;
            PrepareAuditableEntity(doc, isNew: false);
        }

        await _context.SaveChangesAsync();
        return Ok(true);
    }
}