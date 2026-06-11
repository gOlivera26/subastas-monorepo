namespace PortalSubastas.Identity.Application.Services.Implementations;

public class UserService : BaseService, IUserService
{
    private readonly PortalSubastasContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IEmailService _emailService;

    public UserService(
        PortalSubastasContext context,
        IConfiguration configuration,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint,
        IEmailService emailService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
        _emailService = emailService;
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
        var usuario = await _context.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<bool>();
        if (usuario.IdEstado != 4) return BadRequest<bool>("El usuario no está en estado pendiente.");

        usuario.IdEstado = 2; // ACTIVO
        usuario.FechaAprobacion = DateTime.UtcNow;
        usuario.AprobadoPor = GetCurrentUserIdGuid();

        PrepareAuditableEntity(usuario, isNew: false);
        await _context.SaveChangesAsync();

        await _emailService.SendEmailAsync(
            usuario.EmailLogin,
            "Tu cuenta fue aprobada — Trasus Argentina",
            $@"
                <h2>¡Bienvenido a Trasus Argentina!</h2>
                <p>Tu cuenta fue aprobada. Ya podés ingresar al sistema con tu email y contraseña.</p>
                <p>
                    <a href='{_configuration["Frontend:Url"]}/auth/login'
                       style='display:inline-block;padding:12px 24px;background:#2563eb;color:#fff;text-decoration:none;border-radius:6px;'>
                        Ir al inicio de sesión
                    </a>
                </p>
                <hr>
                <small>Trasus Argentina — Portal de Subastas</small>
            ");

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
            .Where(u => u.IdEstado != 4 && u.IdEstado != 8);

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

    public async Task<OperationResponse<bool>> UnlinkUserEntityAsync(Guid userId, LinkEntityRequestDto request)
    {
        var usuario = await _context.TUsuarios
            .Include(u => u.TJurisdiccionesUsuarios)
            .Include(u => u.IdPersonaNavigation).ThenInclude(p => p.TProveedoresRepresentantes)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<bool>();

        bool fueDesvinculado = false;

        if (request.TipoEntidad == "GESTOR")
        {
            var jur = usuario.TJurisdiccionesUsuarios.FirstOrDefault(j => j.IdOrganizacion == request.IdEntidad && j.FecBaja == null);
            if (jur != null)
            {
                PrepareAuditableEntity(jur, false, true);
                _context.TJurisdiccionesUsuarios.Update(jur);
                fueDesvinculado = true;
            }
        }
        else if (request.TipoEntidad == "PROVEEDOR")
        {
            var prov = usuario.IdPersonaNavigation?.TProveedoresRepresentantes.FirstOrDefault(p => p.IdProveedor == request.IdEntidad && p.FecBaja == null);
            if (prov != null)
            {
                PrepareAuditableEntity(prov, false, true);
                _context.TProveedoresRepresentantes.Update(prov);
                fueDesvinculado = true;
            }
        }

        if (!fueDesvinculado) return BadRequest<bool>("No se encontró el vínculo especificado para desvincular.");

        await _context.SaveChangesAsync();
        await PublishSystemLogAsync(_publishEndpoint, "USUARIO_DESVINCULADO", "IAM", new { Mensaje = $"Desvinculación de {usuario.EmailLogin} de {request.TipoEntidad} {request.IdEntidad}" });

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

        if (request.TipoEntidad == "GESTOR")
        {
            if (usuario.TJurisdiccionesUsuarios.Any(j => j.IdOrganizacion == request.IdEntidad && j.FecBaja == null))
                return BadRequest<bool>("El usuario ya se encuentra vinculado a esta organización.");

            var jurisdiccion = new TJurisdiccionesUsuario { IdUsuario = usuario.Id, IdOrganizacion = request.IdEntidad, EsPrincipal = !usuario.TJurisdiccionesUsuarios.Any() };
            PrepareAuditableEntity(jurisdiccion, true);
            _context.TJurisdiccionesUsuarios.Add(jurisdiccion);
        }
        else if (request.TipoEntidad == "PROVEEDOR")
        {
            if (usuario.IdPersonaNavigation?.TProveedoresRepresentantes.Any(p => p.IdProveedor == request.IdEntidad && p.FecBaja == null) == true)
                return BadRequest<bool>("El usuario ya es representante de este proveedor.");

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