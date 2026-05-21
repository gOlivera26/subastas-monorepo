using PortalSubastas.Identity.Application.RequestDto.Vigencia;
using PortalSubastas.Identity.Application.ResponseDto.Vigencia;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class VigenciaService : BaseService, IVigenciaService
{
    private readonly PortalSubastasContext _context;

    public VigenciaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<VigenciaResponseDto>>> GetAllAsync()
    {
        var vigencias = await _context.TVigencias
            .OrderByDescending(v => v.Ejercicio)
            .ToListAsync();

        return Ok(_mapper.Map<List<VigenciaResponseDto>>(vigencias));
    }

    public async Task<OperationResponse<VigenciaResponseDto>> GetByIdAsync(int id)
    {
        var vigencia = await _context.TVigencias.FindAsync(id);
        if (vigencia == null)
            return NotFound<VigenciaResponseDto>();

        return Ok(_mapper.Map<VigenciaResponseDto>(vigencia));
    }

    public async Task<OperationResponse<VigenciaResponseDto>> CreateAsync(VigenciaRequestDto dto)
    {
        var entity = _mapper.Map<TVigencia>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TVigencias.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<VigenciaResponseDto>(entity));
    }

    public async Task<OperationResponse<VigenciaResponseDto>> UpdateAsync(int id, VigenciaRequestDto dto)
    {
        var vigencia = await _context.TVigencias.FindAsync(id);
        if (vigencia == null)
            return NotFound<VigenciaResponseDto>();

        vigencia.Ejercicio = dto.Ejercicio;
        vigencia.ActivoEjecucion = dto.ActivoEjecucion;

        return await UpdateAsync<TVigencia, VigenciaResponseDto>(vigencia, _context);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var vigencia = await _context.TVigencias.FindAsync(id);
        if (vigencia == null)
            return NotFound<bool>();

        return await DeleteAsync(vigencia, _context);
    }

    public async Task<OperationResponse<VigenciaResponseDto>> SetActivaEjecucionAsync(int id)
    {
        var vigencia = await _context.TVigencias.FindAsync(id);
        if (vigencia == null)
            return NotFound<VigenciaResponseDto>();

        var activas = await _context.TVigencias
            .Where(v => v.ActivoEjecucion == true)
            .ToListAsync();

        foreach (var v in activas)
        {
            v.ActivoEjecucion = false;
            PrepareAuditableEntity(v, isNew: false);
        }

        vigencia.ActivoEjecucion = true;
        PrepareAuditableEntity(vigencia, isNew: false);

        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<VigenciaResponseDto>(vigencia));
    }
}
