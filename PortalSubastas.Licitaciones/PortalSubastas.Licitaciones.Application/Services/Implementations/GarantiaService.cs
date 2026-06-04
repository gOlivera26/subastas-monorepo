using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.Garantia;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Garantia;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class GarantiaService : BaseService, IGarantiaService
{
    private new readonly PortalSubastasContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;

    public GarantiaService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IFileStorageService fileStorageService,
        IConfiguration configuration,
        IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<List<GarantiaResponseDto>>> GetByCotizacionAsync(int idCotizacion, int? idProveedor)
    {
        var query = _context.TGarantiasSubastas
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null);

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

        string bucketName = _configuration["CloudflareR2:BucketName"] ?? "subasta-electronica";

   
        string urlArchivo = await _fileStorageService.UploadFileAsync(dto.Archivo, bucketName, "garantias-pagare/");

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

        await PublishSystemLogAsync(_publishEndpoint, "GARANTIA_SUBIDA", "LICITACIONES", new { entity.IdGarantia, entity.IdCotizacion, entity.IdProveedor });

        return Ok(_mapper.Map<GarantiaResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TGarantiasSubastas.FindAsync(id);
        if (entity == null) return NotFound<bool>();

        // Opcional: Borrar el archivo físico de Cloudflare R2 al darlo de baja lógicamente
        // string bucketName = _configuration["CloudflareR2:BucketName"] ?? "subasta-electronica";
        // await _fileStorageService.DeleteFileAsync(entity.UrlArchivo, bucketName);

        var result = await DeleteAsync(entity, _context);
        if (result.Success == true)
            await PublishSystemLogAsync(_publishEndpoint, "GARANTIA_ELIMINADA", "LICITACIONES", new { entity.IdGarantia, entity.IdCotizacion });
        return result;
    }
}