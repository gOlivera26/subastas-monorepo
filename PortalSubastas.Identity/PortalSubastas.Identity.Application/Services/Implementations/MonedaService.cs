using PortalSubastas.Identity.Application.RequestDto.Moneda;
using PortalSubastas.Identity.Application.ResponseDto.Moneda;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class MonedaService : BaseService, IMonedaService
{
    private readonly PortalSubastasContext _context;

    public MonedaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache) { _context = context; }

    public async Task<OperationResponse<List<MonedaResponseDto>>> GetAllAsync()
    {
        var items = await _context.TMonedas.OrderBy(m => m.Nombre).ToListAsync();
        var result = items.Select(m => new MonedaResponseDto
        {
            IdMoneda = m.IdMoneda, Simbolo = m.Simbolo, Nombre = m.Nombre,
            Descripcion = m.Descripcion, Activo = m.FecBaja == null
        }).ToList();
        return Ok(result);
    }

    public async Task<OperationResponse<MonedaResponseDto>> GetByIdAsync(int id)
    {
        var e = await _context.TMonedas.FindAsync(id);
        if (e == null) return NotFound<MonedaResponseDto>();
        return Ok(new MonedaResponseDto { IdMoneda = e.IdMoneda, Simbolo = e.Simbolo, Nombre = e.Nombre, Descripcion = e.Descripcion, Activo = e.FecBaja == null });
    }

    public async Task<OperationResponse<MonedaResponseDto>> CreateAsync(MonedaRequestDto dto)
    {
        var e = _mapper.Map<TMoneda>(dto);
        _context.TMonedas.Add(e); await _context.SaveChangesAsync();
        return Ok(new MonedaResponseDto { IdMoneda = e.IdMoneda, Simbolo = e.Simbolo, Nombre = e.Nombre, Descripcion = e.Descripcion, Activo = true });
    }

    public async Task<OperationResponse<MonedaResponseDto>> UpdateAsync(int id, MonedaRequestDto dto)
    {
        var e = await _context.TMonedas.FindAsync(id);
        if (e == null) return NotFound<MonedaResponseDto>();
        e.Simbolo = dto.Simbolo; e.Nombre = dto.Nombre; e.Descripcion = dto.Descripcion;
        _context.TMonedas.Update(e); await _context.SaveChangesAsync();
        return Ok(new MonedaResponseDto { IdMoneda = e.IdMoneda, Simbolo = e.Simbolo, Nombre = e.Nombre, Descripcion = e.Descripcion, Activo = e.FecBaja == null });
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var e = await _context.TMonedas.FindAsync(id);
        if (e == null) return NotFound<bool>();
        e.FecBaja = DateTime.UtcNow;
        _context.TMonedas.Update(e); await _context.SaveChangesAsync();
        return Ok(true);
    }
}
