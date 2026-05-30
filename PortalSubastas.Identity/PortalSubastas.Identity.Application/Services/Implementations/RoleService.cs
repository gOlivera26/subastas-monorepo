using PortalSubastas.Identity.Application.RequestDto.Role;
using PortalSubastas.Identity.Application.ResponseDto.Role;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class RoleService : BaseService, IRoleService
{
    private readonly PortalSubastasContext _context;

    public RoleService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<RoleResponseDto>>> GetAllAsync()
    {
        var roles = await _context.TRoles.OrderBy(r => r.Nombre).ToListAsync();
        return Ok(_mapper.Map<List<RoleResponseDto>>(roles));
    }

    public async Task<OperationResponse<List<RoleResponseDto>>> GetActiveRolesAsync()
    {
        var roles = await _context.TRoles.ToListAsync();
        return Ok(_mapper.Map<List<RoleResponseDto>>(roles));
    }

    public async Task<OperationResponse<RoleResponseDto>> GetByIdAsync(int id)
    {
        var role = await _context.TRoles.FindAsync(id);
        if (role == null) return NotFound<RoleResponseDto>();
        return Ok(_mapper.Map<RoleResponseDto>(role));
    }

    public async Task<OperationResponse<RoleResponseDto>> CreateAsync(RoleRequestDto dto)
    {
        var entity = _mapper.Map<TRole>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TRoles.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<RoleResponseDto>(entity));
    }

    public async Task<OperationResponse<RoleResponseDto>> UpdateAsync(int id, RoleRequestDto dto)
    {
        var entity = await _context.TRoles.FindAsync(id);
        if (entity == null) return NotFound<RoleResponseDto>();

        entity.Nombre = dto.Nombre;
        entity.Descripcion = dto.Descripcion;
        PrepareAuditableEntity(entity, isNew: false);
        _context.TRoles.Update(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<RoleResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TRoles.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        return await DeleteAsync(entity, _context);
    }

    public async Task<OperationResponse<List<RoleModuleResponseDto>>> GetModulesByRoleAsync(int idRol)
    {
        var modules = await _context.TRolesModulos
            .Include(rm => rm.IdModuloNavigation)
            .Where(rm => rm.IdRol == idRol)
            .Select(rm => new RoleModuleResponseDto
            {
                IdRolModulo = rm.IdRol,
                IdRol = rm.IdRol,
                IdModulo = rm.IdModulo,
                ModuloKeyName = rm.IdModuloNavigation.KeyName,
                ModuloTitulo = rm.IdModuloNavigation.Titulo
            })
            .ToListAsync();

        return Ok(modules);
    }

    public async Task<OperationResponse<RoleModuleResponseDto>> AssignModuleAsync(RoleModuleRequestDto dto)
    {
        var existing = await _context.TRolesModulos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rm => rm.IdRol == dto.IdRol && rm.IdModulo == dto.IdModulo);

        if (existing != null)
        {
            if (existing.FecBaja == null)
                return BadRequest<RoleModuleResponseDto>("El módulo ya está asignado a este rol.");

            existing.FecBaja = null;
            existing.UsrBaja = null;
            PrepareAuditableEntity(existing, isNew: false);
            await _context.SaveChangesAsync();

            var mod = await _context.TModulos.FindAsync(dto.IdModulo);
            return Ok(new RoleModuleResponseDto { IdRol = dto.IdRol, IdModulo = dto.IdModulo, ModuloKeyName = mod?.KeyName ?? "", ModuloTitulo = mod?.Titulo ?? "" });
        }

        var entity = new TRolesModulo { IdRol = dto.IdRol, IdModulo = dto.IdModulo };
        PrepareAuditableEntity(entity, isNew: true);
        _context.TRolesModulos.Add(entity);
        await _context.SaveChangesAsync();

        var modulo = await _context.TModulos.FindAsync(dto.IdModulo);
        return Ok(new RoleModuleResponseDto { IdRol = dto.IdRol, IdModulo = dto.IdModulo, ModuloKeyName = modulo?.KeyName ?? "", ModuloTitulo = modulo?.Titulo ?? "" });
    }

    public async Task<OperationResponse<bool>> UnassignModuleAsync(int idRol, int idModulo)
    {
        var entity = await _context.TRolesModulos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rm => rm.IdRol == idRol && rm.IdModulo == idModulo);

        if (entity == null) return NotFound<bool>();

        _context.TRolesModulos.Remove(entity);

        // Cascada: eliminar también las páginas de este módulo para este rol
        var paginasDelModulo = await _context.TRolesPaginas
            .Where(rp => rp.IdRol == idRol && rp.IdPaginaNavigation.IdModulo == idModulo)
            .ToListAsync();
        _context.TRolesPaginas.RemoveRange(paginasDelModulo);

        await _context.SaveChangesAsync();
        return Ok(true);
    }

    public async Task<OperationResponse<List<ModuloDto>>> GetAllModulesAsync()
    {
        var modulos = await _context.TModulos
            .OrderBy(m => m.Orden)
            .Select(m => new ModuloDto
            {
                Id = m.Id,
                KeyName = m.KeyName,
                Titulo = m.Titulo,
                Descripcion = m.Descripcion,
                Icono = m.IconoLucide,
                Ruta = m.RutaFrontend
            })
            .ToListAsync();

        return Ok(modulos);
    }

    public async Task<OperationResponse<List<PaginaDto>>> GetAllPagesAsync()
    {
        var paginas = await _context.TPaginas
            .Include(p => p.IdModuloNavigation)
            .OrderBy(p => p.IdModuloNavigation.Orden).ThenBy(p => p.KeyName)
            .Select(p => new PaginaDto
            {
                Id = p.Id,
                IdModulo = p.IdModulo,
                ModuloTitulo = p.IdModuloNavigation.Titulo,
                KeyName = p.KeyName,
                Titulo = p.Titulo,
                RutaFrontend = p.RutaFrontend
            })
            .ToListAsync();

        return Ok(paginas);
    }

    public async Task<OperationResponse<List<PaginaDto>>> GetPagesByRoleAsync(int idRol)
    {
        var paginas = await _context.TRolesPaginas
            .Include(rp => rp.IdPaginaNavigation).ThenInclude(p => p.IdModuloNavigation)
            .Where(rp => rp.IdRol == idRol)
            .Select(rp => new PaginaDto
            {
                Id = rp.IdPagina,
                IdModulo = rp.IdPaginaNavigation.IdModulo,
                ModuloTitulo = rp.IdPaginaNavigation.IdModuloNavigation.Titulo,
                KeyName = rp.IdPaginaNavigation.KeyName,
                Titulo = rp.IdPaginaNavigation.Titulo,
                RutaFrontend = rp.IdPaginaNavigation.RutaFrontend
            })
            .ToListAsync();

        return Ok(paginas);
    }

    public async Task<OperationResponse<PaginaDto>> AssignPageAsync(int idRol, int idPagina)
    {
        var exists = await _context.TRolesPaginas.AnyAsync(rp => rp.IdRol == idRol && rp.IdPagina == idPagina);
        if (exists)
            return BadRequest<PaginaDto>("La página ya está asignada a este rol.");

        var entity = new TRolesPagina { IdRol = idRol, IdPagina = idPagina };
        _context.TRolesPaginas.Add(entity);
        await _context.SaveChangesAsync();

        var pagina = await _context.TPaginas.Include(p => p.IdModuloNavigation).FirstOrDefaultAsync(p => p.Id == idPagina);
        return Ok(new PaginaDto
        {
            Id = idPagina,
            IdModulo = pagina?.IdModulo ?? 0,
            ModuloTitulo = pagina?.IdModuloNavigation?.Titulo ?? "",
            KeyName = pagina?.KeyName ?? "",
            Titulo = pagina?.Titulo ?? "",
            RutaFrontend = pagina?.RutaFrontend ?? ""
        });
    }

    public async Task<OperationResponse<bool>> UnassignPageAsync(int idRol, int idPagina)
    {
        var entity = await _context.TRolesPaginas
            .FirstOrDefaultAsync(rp => rp.IdRol == idRol && rp.IdPagina == idPagina);
        if (entity == null) return NotFound<bool>();

        _context.TRolesPaginas.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(true);
    }

    public async Task<OperationResponse<List<ModuloConPaginasDto>>> GetModulosConPaginasAsync()
    {
        var result = await _context.TModulos
            .OrderBy(m => m.Orden)
            .Select(m => new ModuloConPaginasDto
            {
                IdModulo = m.Id,
                ModuloTitulo = m.Titulo,
                Paginas = m.TPaginas
                    .OrderBy(p => p.KeyName)
                    .Select(p => new PaginaDto
                    {
                        Id = p.Id,
                        IdModulo = p.IdModulo,
                        ModuloTitulo = m.Titulo,
                        KeyName = p.KeyName,
                        Titulo = p.Titulo,
                        RutaFrontend = p.RutaFrontend
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(result);
    }
}
