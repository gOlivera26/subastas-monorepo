namespace PortalSubastas.Identity.Application.Services.Implementations;

public class UserService : BaseService, IUserService
{
    private readonly PortalSubastasContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public UserService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<List<PendingUserDto>>> GetPendingUsersAsync()
    {
        var pendingUsers = await _context.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .Include(u => u.TJurisdiccionesUsuarios).ThenInclude(j => j.IdOrganizacionNavigation)
            .Include(u => u.IdPersonaNavigation.TProveedoresRepresentantes).ThenInclude(pr => pr.IdProveedorNavigation)
            .Where(u => u.IdEstado == 4)
            .OrderByDescending(u => u.FecIng)
            .ToListAsync();

        return Ok(_mapper.Map<List<PendingUserDto>>(pendingUsers));
    }

    public async Task<OperationResponse<bool>> ApproveUserAsync(Guid userId)
    {
        var usuario = await _context.TUsuarios.FindAsync(userId);
        if (usuario == null) return NotFound<bool>();
        if (usuario.IdEstado != 4) return BadRequest<bool>("El usuario no está en estado pendiente.");

        usuario.IdEstado = 1; // ACTIVO
        usuario.FechaAprobacion = DateTime.UtcNow;
        usuario.AprobadoPor = GetCurrentUserIdGuid();

        PrepareAuditableEntity(usuario, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "USUARIO_APROBADO", "IAM",
            new { Mensaje = $"Cuenta aprobada el {usuario.FechaAprobacion?.ToLocalTime():dd/MM/yyyy HH:mm}" });

        return Ok(true);
    }

    public async Task<OperationResponse<List<ActiveUserDto>>> GetActiveUsersAsync(int page, int pageSize, string searchTerm, string? sortBy = null, string? sortDirection = null)
    {
        var query = _context.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .Include(u => u.IdRolNavigation)
            .Include(u => u.IdEstadoNavigation)
            .Include(u => u.TJurisdiccionesUsuarios).ThenInclude(j => j.IdOrganizacionNavigation)
            .Include(u => u.IdPersonaNavigation.TProveedoresRepresentantes).ThenInclude(pr => pr.IdProveedorNavigation)
            .Where(u => u.IdEstado != 4);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(u =>
                u.IdPersonaNavigation.Nombre.ToLower().Contains(searchTerm) ||
                u.IdPersonaNavigation.Apellido.ToLower().Contains(searchTerm) ||
                u.IdPersonaNavigation.NroDocumento.Contains(searchTerm) ||
                u.EmailLogin.ToLower().Contains(searchTerm));
        }

        query = (sortBy, sortDirection?.ToLower()) switch
        {
            ("nombreCompleto", "desc") => query.OrderByDescending(u => u.IdPersonaNavigation.Apellido),
            ("nombreCompleto", _) => query.OrderBy(u => u.IdPersonaNavigation.Apellido),
            ("estado", "desc") => query.OrderByDescending(u => u.IdEstadoNavigation.Descripcion),
            ("estado", _) => query.OrderBy(u => u.IdEstadoNavigation.Descripcion),
            ("documento", "desc") => query.OrderByDescending(u => u.IdPersonaNavigation.NroDocumento),
            ("documento", _) => query.OrderBy(u => u.IdPersonaNavigation.NroDocumento),
            _ => query.OrderBy(u => u.IdPersonaNavigation.Apellido)
        };

        var result = await GetPagedDataAsync<TUsuario, ActiveUserDto>(page, pageSize, query);
        var (data, total) = result.Data;

        return Ok(data, total);
    }

    public async Task<OperationResponse<UserAuditDto>> GetUserAuditAsync(Guid userId)
    {
        var usuario = await _context.TUsuarios
            .Include(u => u.AprobadoPorNavigation).ThenInclude(a => a.IdPersonaNavigation)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<UserAuditDto>();

        return Ok(_mapper.Map<UserAuditDto>(usuario));
    }

    public async Task<OperationResponse<string>> ResetUserPasswordAsync(Guid userId)
    {
        var usuario = await _context.TUsuarios.FindAsync(userId);
        if (usuario == null) return NotFound<string>();

        string tempPassword = Guid.NewGuid().ToString("N").Substring(0, 8);
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        PrepareAuditableEntity(usuario, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RESETEO_PASSWORD", "IAM",
    new { Mensaje = $"Un administrador reseteó la contraseña del usuario {usuario.EmailLogin}" });

        return Ok(tempPassword);
    }

    public async Task<OperationResponse<bool>> UnlinkUserEntityAsync(Guid userId)
    {
        var usuario = await _context.TUsuarios
            .Include(u => u.TJurisdiccionesUsuarios)
            .Include(u => u.IdPersonaNavigation).ThenInclude(p => p.TProveedoresRepresentantes)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<bool>();

        bool fueDesvinculado = false;
        if (usuario.TJurisdiccionesUsuarios.Any())
        {
            foreach (var j in usuario.TJurisdiccionesUsuarios) PrepareAuditableEntity(j, false, true);
            _context.TJurisdiccionesUsuarios.UpdateRange(usuario.TJurisdiccionesUsuarios);
            fueDesvinculado = true;
        }
        if (usuario.IdPersonaNavigation?.TProveedoresRepresentantes.Any() == true)
        {
            foreach (var r in usuario.IdPersonaNavigation.TProveedoresRepresentantes) PrepareAuditableEntity(r, false, true);
            _context.TProveedoresRepresentantes.UpdateRange(usuario.IdPersonaNavigation.TProveedoresRepresentantes);
            fueDesvinculado = true;
        }

        if (!fueDesvinculado) return BadRequest<bool>("El usuario no tiene vínculos.");

        await _context.SaveChangesAsync();
        await PublishSystemLogAsync(_publishEndpoint, "USUARIO_DESVINCULADO", "IAM", new { Mensaje = $"Desvinculación de {usuario.EmailLogin}" });

        return Ok(true);
    }

    public async Task<OperationResponse<bool>> UpdateUserRoleAsync(Guid userId, int newRoleId)
    {
        var usuario = await _context.TUsuarios.FindAsync(userId);
        if (usuario == null) return NotFound<bool>();

        if (!await _context.TRoles.AnyAsync(r => r.Id == newRoleId)) return BadRequest<bool>("Rol inválido.");

        usuario.IdRol = newRoleId;
        PrepareAuditableEntity(usuario, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "CAMBIO_ROL", "IAM",
    new { Mensaje = $"Se actualizó el rol del usuario {usuario.EmailLogin} al rol ID: {newRoleId}" });

        return Ok(true);
    }

    public async Task<OperationResponse<bool>> LinkUserEntityAsync(Guid userId, LinkEntityRequestDto request)
    {
        var usuario = await _context.TUsuarios
            .Include(u => u.TJurisdiccionesUsuarios)
            .Include(u => u.IdPersonaNavigation).ThenInclude(p => p.TProveedoresRepresentantes)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<bool>();

        if (usuario.TJurisdiccionesUsuarios.Any() || usuario.IdPersonaNavigation?.TProveedoresRepresentantes.Any() == true)
            return BadRequest<bool>("El usuario ya tiene una entidad vinculada.");

        if (request.TipoEntidad == "GESTOR")
        {
            var jurisdiccion = new TJurisdiccionesUsuario { IdUsuario = usuario.Id, IdOrganizacion = request.IdEntidad, EsPrincipal = true };
            PrepareAuditableEntity(jurisdiccion, true);
            _context.TJurisdiccionesUsuarios.Add(jurisdiccion);
        }
        else if (request.TipoEntidad == "PROVEEDOR")
        {
            var representante = new TProveedoresRepresentante { IdPersona = usuario.IdPersona, IdProveedor = request.IdEntidad, EsApoderado = false };
            PrepareAuditableEntity(representante, true);
            _context.TProveedoresRepresentantes.Add(representante);
        }

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "USUARIO_VINCULADO", "IAM",
    new { Mensaje = $"Se vinculó el usuario {usuario.EmailLogin} como {request.TipoEntidad} (ID: {request.IdEntidad})" });

        return Ok(true);
    }
}