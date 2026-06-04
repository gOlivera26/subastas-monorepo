using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations
{
    public class CotizacionDocumentoService : BaseService, ICotizacionDocumentoService
    {
        private new readonly PortalSubastasContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IConfiguration _configuration;
        private readonly IPublishEndpoint _publishEndpoint;

        public CotizacionDocumentoService(
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

        public async Task<OperationResponse<List<CotizacionDocumentoResponseDto>>> GetByCotizacionAsync(int idCotizacion)
        {
            var documentos = await _context.TCotizacionDocumentos
                .Where(d => d.IdCotizacion == idCotizacion && d.FecBaja == null)
                .OrderByDescending(d => d.FecIng)
                .ToListAsync();

            return Ok(_mapper.Map<List<CotizacionDocumentoResponseDto>>(documentos));
        }

        public async Task<OperationResponse<CotizacionDocumentoResponseDto>> CreateAsync(CotizacionDocumentoRequestDto dto)
        {
            if (dto.Archivo == null || dto.Archivo.Length == 0)
                return BadRequest<CotizacionDocumentoResponseDto>("El archivo es obligatorio.");

            if (dto.Archivo.Length > 20 * 1024 * 1024)
                return BadRequest<CotizacionDocumentoResponseDto>("El archivo pliego no puede superar los 20 MB.");

            var cotizacion = await _context.TCotizaciones.AnyAsync(c => c.IdCotizacion == dto.IdCotizacion);
            if (!cotizacion) return NotFound<CotizacionDocumentoResponseDto>();

            string bucketName = _configuration["CloudflareR2:BucketName"] ?? "subasta-electronica";

            string urlArchivo = await _fileStorageService.UploadFileAsync(dto.Archivo, bucketName, "pliegos/");

            var entity = new TCotizacionDocumento
            {
                IdCotizacion = dto.IdCotizacion,
                TipoDocumento = dto.TipoDocumento.ToUpper(),
                NombreArchivo = dto.Archivo.FileName,
                UrlArchivo = urlArchivo
            };

            PrepareAuditableEntity(entity, isNew: true);

            _context.TCotizacionDocumentos.Add(entity);
            await _context.SaveChangesAsync();

            await PublishSystemLogAsync(_publishEndpoint, "PLIEGO_SUBIDO", "LICITACIONES", new { entity.IdCotDocumento, entity.IdCotizacion, entity.TipoDocumento, entity.NombreArchivo });

            return Ok(_mapper.Map<CotizacionDocumentoResponseDto>(entity));
        }

        public async Task<OperationResponse<bool>> DeleteAsync(int id)
        {
            var entity = await _context.TCotizacionDocumentos.FindAsync(id);
            if (entity == null) return NotFound<bool>();

            var result = await DeleteAsync(entity, _context);
            if (result.Success == true)
                await PublishSystemLogAsync(_publishEndpoint, "PLIEGO_ELIMINADO", "LICITACIONES", new { entity.IdCotDocumento, entity.IdCotizacion });
            return result;
        }
    }
}
