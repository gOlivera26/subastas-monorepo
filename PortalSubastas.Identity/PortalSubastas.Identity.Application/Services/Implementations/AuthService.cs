namespace PortalSubastas.Identity.Application.Services.Implementations;

public class AuthService : BaseService, IAuthService
{
    private readonly PortalSubastasContext _identityContext;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IEmailService _emailService;

    public AuthService(
    PortalSubastasContext context,
    IConfiguration configuration,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache,
    IPublishEndpoint publishEndpoint,
    IEmailService emailService)
    : base(context, mapper, httpContextAccessor, cache)
    {
        _identityContext = context;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
        _emailService = emailService;
    }

    public async Task<OperationResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdPersonaNavigation).ThenInclude(p => p.TProveedoresRepresentantes).ThenInclude(pr => pr.IdProveedorNavigation)
            .Include(u => u.TJurisdiccionesUsuarios).ThenInclude(j => j.IdOrganizacionNavigation)
            .Include(u => u.IdRolNavigation)
            .Include(u => u.IdEstadoNavigation)
            .FirstOrDefaultAsync(u => u.EmailLogin == request.Email);

        if (usuario == null)
            return Unauthorized<LoginResponseDto>("Credenciales incorrectas.");

        if (usuario.IdEstadoNavigation.Descripcion != "ACTIVO")
            return Unauthorized<LoginResponseDto>("El usuario se encuentra inactivo o bloqueado.");

        if (!BC.Verify(request.Password, usuario.PasswordHash))
            return Unauthorized<LoginResponseDto>("Credenciales incorrectas.");

        var modulosPermitidos = await _identityContext.TRolesModulos
            .Include(rm => rm.IdModuloNavigation)
            .Where(rm => rm.IdRol == usuario.IdRol && rm.FecBaja == null)
            .Select(rm => new ModuloDto
            {
                Id = rm.IdModuloNavigation.Id,
                KeyName = rm.IdModuloNavigation.KeyName,
                Titulo = rm.IdModuloNavigation.Titulo,
                Descripcion = rm.IdModuloNavigation.Descripcion,
                Icono = rm.IdModuloNavigation.IconoLucide,
                Ruta = rm.IdModuloNavigation.RutaFrontend
            })
            .ToListAsync();

        var modulosDesdePaginas = await _identityContext.TRolesPaginas
            .Include(rp => rp.IdPaginaNavigation).ThenInclude(p => p.IdModuloNavigation)
            .Where(rp => rp.IdRol == usuario.IdRol)
            .Select(rp => rp.IdPaginaNavigation.IdModuloNavigation)
            .Distinct()
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

        modulosPermitidos = modulosPermitidos
            .UnionBy(modulosDesdePaginas, m => m.Id)
            .OrderBy(m => modulosPermitidos.Concat(modulosDesdePaginas).ToList().IndexOf(m))
            .ToList();

        var entidades = new List<EntidadDto>();
        foreach (var j in usuario.TJurisdiccionesUsuarios.Where(x => x.FecBaja == null))
            entidades.Add(new EntidadDto { Id = j.IdOrganizacion, Tipo = "GESTOR", Nombre = j.IdOrganizacionNavigation.Nombre });
        foreach (var p in usuario.IdPersonaNavigation.TProveedoresRepresentantes.Where(x => x.FecBaja == null))
            entidades.Add(new EntidadDto { Id = p.IdProveedor, Tipo = "PROVEEDOR", Nombre = p.IdProveedorNavigation.RazonSocial });

        var token = GenerarJwtToken(usuario); // Llama por defecto al primer contexto

        usuario.UltimoAcceso = DateTime.UtcNow;
        _identityContext.TUsuarios.Update(usuario);
        await _identityContext.SaveChangesAsync();

        var response = new LoginResponseDto
        {
            Token = token,
            NombreUsuario = $"{usuario.IdPersonaNavigation.Nombre} {usuario.IdPersonaNavigation.Apellido}",
            Email = usuario.EmailLogin,
            Rol = usuario.IdRolNavigation.Nombre,
            Modulos = modulosPermitidos,
            Entidades = entidades,
            Paginas = await _identityContext.TRolesPaginas
                .Include(rp => rp.IdPaginaNavigation).ThenInclude(p => p.IdModuloNavigation)
                .Where(rp => rp.IdRol == usuario.IdRol)
                .Select(rp => new PortalSubastas.Identity.Application.ResponseDto.Role.PaginaDto
                {
                    Id = rp.IdPagina,
                    IdModulo = rp.IdPaginaNavigation.IdModulo,
                    ModuloTitulo = rp.IdPaginaNavigation.IdModuloNavigation.Titulo,
                    KeyName = rp.IdPaginaNavigation.KeyName,
                    Titulo = rp.IdPaginaNavigation.Titulo,
                    RutaFrontend = rp.IdPaginaNavigation.RutaFrontend
                }).ToListAsync()
        };

        await PublishSystemLogAsync(_publishEndpoint, "INICIO_SESION", "IAM", new { Mensaje = $"El usuario {usuario.EmailLogin} inició sesión exitosamente." });

        return Ok(response);
    }

    public async Task<OperationResponse<LoginResponseDto>> SwitchContextAsync(SwitchContextRequestDto request)
    {
        var userId = GetCurrentUserIdGuid();
        if (userId == null) return Unauthorized<LoginResponseDto>();

        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<LoginResponseDto>();

        // Validar que realmente pertenece a esa entidad
        if (request.TipoEntidad == "GESTOR")
        {
            var existe = await _identityContext.TJurisdiccionesUsuarios.AnyAsync(j => j.IdUsuario == userId && j.IdOrganizacion == request.IdEntidad && j.FecBaja == null);
            if (!existe) return BadRequest<LoginResponseDto>("No perteneces a esta organización.");
        }
        else if (request.TipoEntidad == "PROVEEDOR")
        {
            var existe = await _identityContext.TProveedoresRepresentantes.AnyAsync(p => p.IdPersona == usuario.IdPersona && p.IdProveedor == request.IdEntidad && p.FecBaja == null);
            if (!existe) return BadRequest<LoginResponseDto>("No eres representante de este proveedor.");
        }

        int? idOrg = request.TipoEntidad == "GESTOR" ? request.IdEntidad : null;
        int? idProv = request.TipoEntidad == "PROVEEDOR" ? request.IdEntidad : null;

        var token = GenerarJwtToken(usuario, idOrg, idProv);

        var response = new LoginResponseDto
        {
            Token = token,
            NombreUsuario = $"{usuario.IdPersonaNavigation.Nombre} {usuario.IdPersonaNavigation.Apellido}",
            Email = usuario.EmailLogin,
            Rol = usuario.IdRolNavigation.Nombre
        };

        return Ok(response);
    }

    public async Task<OperationResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var emailExiste = await _identityContext.TUsuarios.AnyAsync(u => u.EmailLogin == request.Email);
        if (emailExiste)
            return BadRequest<LoginResponseDto>("El correo electrónico ya se encuentra registrado.");

        var docExiste = await _identityContext.TPersonas.AnyAsync(p => p.NroDocumento == request.NroDocumento);
        if (docExiste)
            return BadRequest<LoginResponseDto>("El documento ya se encuentra registrado.");

        string? codigoConfirmacion = null;

        var transactionResult = await InsertWithTransactionAsync(async () =>
        {
            var persona = new TPersona
            {
                IdTipoPersona = request.IdTipoPersona,
                IdTipoDocumento = request.IdTipoDocumento,
                NroDocumento = request.NroDocumento,
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                EmailContacto = request.Email,
                Telefono = string.Empty
            };

            PrepareAuditableEntity(persona, isNew: true);
            _identityContext.TPersonas.Add(persona);
            await _identityContext.SaveChangesAsync();

            codigoConfirmacion = GenerarCodigoConfirmacion();
            var usuario = new TUsuario
            {
                IdPersona = persona.Id,
                IdRol = request.IdRol,
                IdEstado = 8, // PENDIENTE_CONFIRMACION
                EmailLogin = request.Email,
                PasswordHash = BC.HashPassword(request.Password),
                CodigoConfirmacion = codigoConfirmacion,
                EmailConfirmado = false,
                FechaEnvioCodigo = DateTime.UtcNow
            };

            PrepareAuditableEntity(usuario, isNew: true);
            _identityContext.TUsuarios.Add(usuario);
            await _identityContext.SaveChangesAsync();

            if (request.IdOrganizacion.HasValue)
            {
                var jurisdiccion = new TJurisdiccionesUsuario
                {
                    IdUsuario = usuario.Id,
                    IdOrganizacion = request.IdOrganizacion.Value,
                    EsPrincipal = true
                };
                PrepareAuditableEntity(jurisdiccion, isNew: true);
                _identityContext.TJurisdiccionesUsuarios.Add(jurisdiccion);
            }
            else if (request.IdProveedor.HasValue)
            {
                var representante = new TProveedoresRepresentante
                {
                    IdProveedor = request.IdProveedor.Value,
                    IdPersona = persona.Id,
                    EsApoderado = false
                };
                PrepareAuditableEntity(representante, isNew: true);
                _identityContext.TProveedoresRepresentantes.Add(representante);
            }
        }, _identityContext);

        if (transactionResult.Success != true)
        {
            return InternalServerError<LoginResponseDto>($"Error al procesar el registro: {transactionResult.Message}");
        }

        await _emailService.SendEmailAsync(
            request.Email,
            "Confirmá tu correo electrónico — Trasus Argentina",
            $@"
                <h2>Gracias por registrarte</h2>
                <p>Tu código de confirmación es:</p>
                <h1 style='font-size:32px;letter-spacing:6px;background:#f4f4f4;padding:12px;text-align:center;border-radius:8px;'>{codigoConfirmacion}</h1>
                <p>Este código expira en 30 minutos.</p>
                <p>Si no solicitaste este registro, ignorá este mensaje.</p>
                <hr>
                <small>Trasus Argentina — Portal de Subastas</small>
            ");

        await PublishSystemLogAsync(_publishEndpoint, "NUEVO_REGISTRO", "IAM",
    new { Mensaje = $"Nuevo usuario registrado en el sistema: {request.Email} (Documento: {request.NroDocumento})" });

        return Ok(new LoginResponseDto
        {
            NombreUsuario = $"{request.Nombre} {request.Apellido}",
            Email = request.Email,
            Token = string.Empty
        });
    }

    public async Task<OperationResponse<ProfileResponseDto>> GetProfileAsync()
    {
        var userId = GetCurrentUserIdGuid();
        if (userId == null) return Unauthorized<ProfileResponseDto>();

        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<ProfileResponseDto>();

        return Ok(_mapper.Map<ProfileResponseDto>(usuario));
    }

    public async Task<OperationResponse<bool>> ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        var userId = GetCurrentUserIdGuid();
        if (userId == null) return Unauthorized<bool>();

        var usuario = await _identityContext.TUsuarios.FindAsync(userId);
        if (usuario == null) return NotFound<bool>();

        if (!BC.Verify(request.PasswordActual, usuario.PasswordHash))
            return BadRequest<bool>("La contraseña actual es incorrecta.");

        usuario.PasswordHash = BC.HashPassword(request.NuevaPassword);
        PrepareAuditableEntity(usuario, isNew: false);
        _identityContext.TUsuarios.Update(usuario);
        await _identityContext.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "CAMBIO_PASSWORD", "IAM",
    new { Mensaje = $"El usuario {usuario.EmailLogin} modificó su contraseña personal." });

        return Ok(true);
    }

    public async Task<OperationResponse<ProfileResponseDto>> UpdateProfileAsync(UpdateProfileRequestDto request)
    {
        var userId = GetCurrentUserIdGuid();
        if (userId == null) return Unauthorized<ProfileResponseDto>();

        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound<ProfileResponseDto>();

        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest<ProfileResponseDto>("El nombre y el apellido son obligatorios.");

        usuario.IdPersonaNavigation.Nombre = request.Nombre;
        usuario.IdPersonaNavigation.Apellido = request.Apellido;
        usuario.IdPersonaNavigation.Telefono = request.Telefono;

        PrepareAuditableEntity(usuario.IdPersonaNavigation, isNew: false);
        _identityContext.TPersonas.Update(usuario.IdPersonaNavigation);
        await _identityContext.SaveChangesAsync();

        return Ok(_mapper.Map<ProfileResponseDto>(usuario));
    }


    private string GenerarJwtToken(TUsuario usuario, int? idOrganizacionContexto = null, int? idProveedorContexto = null)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.EmailLogin),
            new(ClaimTypes.Name, $"{usuario.IdPersonaNavigation.Nombre} {usuario.IdPersonaNavigation.Apellido}"),
            new(ClaimTypes.Role, usuario.IdRolNavigation.Nombre)
        };

        // LÓGICA DE CONTEXTO ESTRICTO (O uno, o el otro)
        if (idOrganizacionContexto.HasValue)
        {
            claims.Add(new Claim("IdOrganizacion", idOrganizacionContexto.Value.ToString()));
            claims.Add(new Claim("TipoContexto", "GESTOR"));
        }
        else if (idProveedorContexto.HasValue)
        {
            claims.Add(new Claim("IdProveedor", idProveedorContexto.Value.ToString()));
            claims.Add(new Claim("TipoContexto", "PROVEEDOR"));
        }
        else
        {
            var orgPrincipal = _identityContext.TJurisdiccionesUsuarios.FirstOrDefault(j => j.IdUsuario == usuario.Id && j.FecBaja == null && j.EsPrincipal == true)
                            ?? _identityContext.TJurisdiccionesUsuarios.FirstOrDefault(j => j.IdUsuario == usuario.Id && j.FecBaja == null);

            if (orgPrincipal != null)
            {
                claims.Add(new Claim("IdOrganizacion", orgPrincipal.IdOrganizacion.ToString()));
                claims.Add(new Claim("TipoContexto", "GESTOR"));
            }
            else
            {
                var proveedor = _identityContext.TProveedoresRepresentantes.FirstOrDefault(pr => pr.IdPersona == usuario.IdPersona && pr.FecBaja == null);
                if (proveedor != null)
                {
                    claims.Add(new Claim("IdProveedor", proveedor.IdProveedor.ToString()));
                    claims.Add(new Claim("TipoContexto", "PROVEEDOR"));
                }
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["Minutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<OperationResponse<bool>> ConfirmarEmailAsync(ConfirmEmailRequestDto request)
    {
        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(u => u.EmailLogin == request.Email);

        if (usuario == null)
            return NotFound<bool>();

        if (usuario.IdEstado != 8)
            return BadRequest<bool>("El usuario no está pendiente de confirmación de email.");

        if (usuario.CodigoConfirmacion != request.Codigo)
            return BadRequest<bool>("El código de confirmación es incorrecto.");

        if (usuario.FechaEnvioCodigo.HasValue &&
            DateTime.UtcNow > usuario.FechaEnvioCodigo.Value.AddMinutes(30))
            return BadRequest<bool>("El código de confirmación expiró. Solicitá uno nuevo.");

        usuario.IdEstado = 4; // PENDIENTE (esperando aprobación del admin)
        usuario.EmailConfirmado = true;
        usuario.CodigoConfirmacion = null;
        usuario.FechaEnvioCodigo = null;

        PrepareAuditableEntity(usuario, isNew: false);
        await _identityContext.SaveChangesAsync();

        // Notificar a todos los SUPERADMIN
        await NotificarSuperadminsNuevoUsuarioAsync(usuario);

        await PublishSystemLogAsync(_publishEndpoint, "EMAIL_CONFIRMADO", "IAM",
            new { Mensaje = $"El usuario {usuario.EmailLogin} confirmó su correo electrónico." });

        return Ok(true);
    }

    public async Task<OperationResponse<bool>> ReenviarCodigoAsync(string email)
    {
        var usuario = await _identityContext.TUsuarios.FirstOrDefaultAsync(u => u.EmailLogin == email);

        if (usuario == null)
            return NotFound<bool>();

        if (usuario.IdEstado != 8)
            return BadRequest<bool>("El usuario no está pendiente de confirmación de email.");

        var nuevoCodigo = GenerarCodigoConfirmacion();
        usuario.CodigoConfirmacion = nuevoCodigo;
        usuario.FechaEnvioCodigo = DateTime.UtcNow;

        PrepareAuditableEntity(usuario, isNew: false);
        await _identityContext.SaveChangesAsync();

        await _emailService.SendEmailAsync(
            email,
            "Nuevo código de confirmación — Trasus Argentina",
            $@"
                <h2>Acá va tu nuevo código</h2>
                <h1 style='font-size:32px;letter-spacing:6px;background:#f4f4f4;padding:12px;text-align:center;border-radius:8px;'>{nuevoCodigo}</h1>
                <p>Este código expira en 30 minutos.</p>
                <p>Si no solicitaste este registro, ignorá este mensaje.</p>
                <hr>
                <small>Trasus Argentina — Portal de Subastas</small>
            ");

        return Ok(true);
    }

    private static string GenerarCodigoConfirmacion()
        => Random.Shared.Next(100_000, 999_999).ToString();

    private async Task NotificarSuperadminsNuevoUsuarioAsync(TUsuario usuario)
    {
        var rolSuperadmin = await _identityContext.TRoles
            .FirstOrDefaultAsync(r => r.Nombre == "SUPERADMIN");

        if (rolSuperadmin == null) return;

        var superadmins = await _identityContext.TUsuarios
            .Include(u => u.IdPersonaNavigation)
            .Where(u => u.IdRol == rolSuperadmin.Id && u.FecBaja == null)
            .ToListAsync();

        foreach (var admin in superadmins)
        {
            await _emailService.SendEmailAsync(
                admin.EmailLogin,
                "Nuevo usuario pendiente de aprobación — Trasus Argentina",
                $@"
                    <h2>Nuevo registro en el sistema</h2>
                    <p>El usuario <strong>{usuario.IdPersonaNavigation?.Nombre} {usuario.IdPersonaNavigation?.Apellido}</strong>
                    ({usuario.EmailLogin}) confirmó su correo y está esperando aprobación.</p>
                    <p>Ingresá al panel de administración para revisar la solicitud.</p>
                    <hr>
                    <small>Trasus Argentina — Portal de Subastas</small>
                ");
        }
    }
}
