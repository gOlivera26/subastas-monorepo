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

        var entidades = ObtenerEntidadesOperables(usuario);
        var token = GenerarJwtToken(usuario);

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

        request.TipoEntidad = NormalizarTipoContexto(request.TipoEntidad);
        if (!EsTipoContextoValido(request.TipoEntidad))
            return BadRequest<LoginResponseDto>("El tipo de contexto solicitado no es válido.");

        if (!RolPuedeOperarContexto(usuario.IdRolNavigation.Nombre, request.TipoEntidad))
            return BadRequest<LoginResponseDto>("El rol del usuario no permite operar en este contexto.");

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
        var contextoRegistro = ObtenerTipoContextoRegistro(request);
        if (contextoRegistro == null)
            return BadRequest<LoginResponseDto>("Debe seleccionar una organización o un proveedor, pero no ambos.");

        var emailExiste = await _identityContext.TUsuarios.AnyAsync(u => u.EmailLogin == request.Email);
        if (emailExiste)
            return BadRequest<LoginResponseDto>("El correo electrónico ya se encuentra registrado.");

        var docExiste = await _identityContext.TPersonas.AnyAsync(p => p.NroDocumento == request.NroDocumento);
        if (docExiste)
            return BadRequest<LoginResponseDto>("El documento ya se encuentra registrado.");

        var rol = await _identityContext.TRoles.FirstOrDefaultAsync(r => r.Id == request.IdRol);
        if (rol == null)
        {
            rol = await ResolveRegistrationRoleAsync(request);
            if (rol == null)
                return BadRequest<LoginResponseDto>("El rol seleccionado no existe o no está activo.");
        }

        var idRolRegistro = rol.Id;

        if (!RolPuedeOperarContexto(rol.Nombre, contextoRegistro))
            return BadRequest<LoginResponseDto>("El rol seleccionado no corresponde al tipo de registro.");

        var tipoPersonaExiste = await _identityContext.TTiposPersonas.AnyAsync(t => t.Id == request.IdTipoPersona);
        if (!tipoPersonaExiste)
            return BadRequest<LoginResponseDto>("El tipo de persona seleccionado no existe o no está activo.");

        var tipoDocumentoExiste = await _identityContext.TTiposDocumentos.AnyAsync(t => t.Id == request.IdTipoDocumento);
        if (!tipoDocumentoExiste)
            return BadRequest<LoginResponseDto>("El tipo de documento seleccionado no existe o no está activo.");

        if (contextoRegistro == "GESTOR")
        {
            var organizacionExiste = await _identityContext.TOrganizaciones
                .AnyAsync(o => o.IdOrganizacion == request.IdOrganizacion!.Value && o.Activo == true);
            if (!organizacionExiste)
                return BadRequest<LoginResponseDto>("La organización seleccionada no existe o no está activa.");
        }

        if (contextoRegistro == "PROVEEDOR")
        {
            var proveedorExiste = await _identityContext.TProveedores
                .AnyAsync(p => p.Id == request.IdProveedor!.Value);
            if (!proveedorExiste)
                return BadRequest<LoginResponseDto>("El proveedor seleccionado no existe o no está activo.");
        }

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
                IdRol = idRolRegistro,
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

            if (contextoRegistro == "GESTOR")
            {
                var jurisdiccion = new TJurisdiccionesUsuario
                {
                    IdUsuario = usuario.Id,
                    IdOrganizacion = request.IdOrganizacion!.Value,
                    EsPrincipal = true
                };
                PrepareAuditableEntity(jurisdiccion, isNew: true);
                _identityContext.TJurisdiccionesUsuarios.Add(jurisdiccion);
            }
            else if (contextoRegistro == "PROVEEDOR")
            {
                var representante = new TProveedoresRepresentante
                {
                    IdProveedor = request.IdProveedor!.Value,
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
            "Confirmá tu correo electrónico — OWEN",
            EmailTemplateHelper.GetCodigoVerificationEmail(
                "Verificación de cuenta",
                "Gracias por registrarte en la plataforma. Utilizá el siguiente código para confirmar tu correo:",
                codigoConfirmacion!)
        );

        await PublishSystemLogAsync(_publishEndpoint, "NUEVO_REGISTRO", "IAM",
            new { Mensaje = $"Nuevo usuario registrado en el sistema: {request.Email} (Documento: {request.NroDocumento})" });

        return Ok(new LoginResponseDto
        {
            NombreUsuario = $"{request.Nombre} {request.Apellido}",
            Email = request.Email,
            Token = string.Empty,
            CodigoConfirmacionDesarrollo = ShouldExposeDevEmailCode() ? codigoConfirmacion : null
        });
    }

    private async Task<TRole?> ResolveRegistrationRoleAsync(RegisterRequestDto request)
    {
        var roles = await _identityContext.TRoles.AsNoTracking().ToListAsync();

        var contextoRegistro = ObtenerTipoContextoRegistro(request);

        if (contextoRegistro == "GESTOR")
        {
            return roles.FirstOrDefault(r =>
                RoleNameContains(r.Nombre, "GESTOR") ||
                RoleNameContains(r.Nombre, "LICITACION") ||
                RoleNameContains(r.Nombre, "LICITACIÓN") ||
                RoleNameContains(r.Nombre, "INVERSA"));
        }

        if (contextoRegistro == "PROVEEDOR")
        {
            return roles.FirstOrDefault(r =>
                RoleNameContains(r.Nombre, "PROVEEDOR") ||
                RoleNameContains(r.Nombre, "DIRECTA"));
        }

        return null;
    }

    private static bool RoleNameContains(string? roleName, string value)
        => !string.IsNullOrWhiteSpace(roleName) &&
           roleName.Trim().ToUpperInvariant().Contains(value, StringComparison.OrdinalIgnoreCase);

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
            if (RolPuedeOperarContexto(usuario.IdRolNavigation.Nombre, "GESTOR"))
            {
                var orgPrincipal = _identityContext.TJurisdiccionesUsuarios.FirstOrDefault(j => j.IdUsuario == usuario.Id && j.FecBaja == null && j.EsPrincipal == true)
                                ?? _identityContext.TJurisdiccionesUsuarios.FirstOrDefault(j => j.IdUsuario == usuario.Id && j.FecBaja == null);

                if (orgPrincipal != null)
                {
                    claims.Add(new Claim("IdOrganizacion", orgPrincipal.IdOrganizacion.ToString()));
                    claims.Add(new Claim("TipoContexto", "GESTOR"));
                }
            }

            if (!claims.Any(c => c.Type == "TipoContexto") && RolPuedeOperarContexto(usuario.IdRolNavigation.Nombre, "PROVEEDOR"))
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

    private static string NormalizarTipoContexto(string? tipo)
        => (tipo ?? string.Empty).Trim().ToUpperInvariant();

    private static bool EsTipoContextoValido(string tipo)
        => tipo is "GESTOR" or "PROVEEDOR";

    private static string? ObtenerTipoContextoRegistro(RegisterRequestDto request)
    {
        var tieneOrganizacion = request.IdOrganizacion.HasValue;
        var tieneProveedor = request.IdProveedor.HasValue;

        if (tieneOrganizacion == tieneProveedor)
            return null;

        return tieneOrganizacion ? "GESTOR" : "PROVEEDOR";
    }

    private static bool RolPuedeOperarContexto(string? roleName, string tipoContexto)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        if (RoleNameContains(roleName, "SUPERADMIN"))
            return true;

        return tipoContexto switch
        {
            "GESTOR" =>
                RoleNameContains(roleName, "GESTOR") ||
                RoleNameContains(roleName, "OPERADOR") ||
                RoleNameContains(roleName, "ADMIN") ||
                RoleNameContains(roleName, "LICITACION") ||
                RoleNameContains(roleName, "LICITACIÓN") ||
                RoleNameContains(roleName, "INVERSA"),
            "PROVEEDOR" =>
                RoleNameContains(roleName, "PROVEEDOR") ||
                RoleNameContains(roleName, "DIRECTA"),
            _ => false
        };
    }

    private static List<EntidadDto> ObtenerEntidadesOperables(TUsuario usuario)
    {
        var entidades = new List<EntidadDto>();
        var rol = usuario.IdRolNavigation.Nombre;

        if (RolPuedeOperarContexto(rol, "GESTOR"))
        {
            foreach (var j in usuario.TJurisdiccionesUsuarios.Where(x => x.FecBaja == null))
                entidades.Add(new EntidadDto { Id = j.IdOrganizacion, Tipo = "GESTOR", Nombre = j.IdOrganizacionNavigation.Nombre });
        }

        if (RolPuedeOperarContexto(rol, "PROVEEDOR"))
        {
            foreach (var p in usuario.IdPersonaNavigation.TProveedoresRepresentantes.Where(x => x.FecBaja == null))
                entidades.Add(new EntidadDto { Id = p.IdProveedor, Tipo = "PROVEEDOR", Nombre = p.IdProveedorNavigation.RazonSocial });
        }

        return entidades;
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
            "Nuevo código de confirmación — OWEN",
            EmailTemplateHelper.GetCodigoVerificationEmail(
                "Nuevo código generado",
                "Solicitaste un nuevo código de verificación. Utilizá el siguiente código para confirmar tu correo:",
                nuevoCodigo)
        );

        if (ShouldExposeDevEmailCode())
        {
            return OperationResponse<bool>.CreateBuilder()
                .WithSuccess(true)
                .WithMessage($"Codigo de confirmacion desarrollo local: {nuevoCodigo}")
                .WithData(true)
                .WithCode(200)
                .Build();
        }

        return Ok(true);
    }

    public async Task<OperationResponse<bool>> SolicitarResetPasswordAsync(SolicitarResetRequestDto request)
    {
        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdEstadoNavigation)
            .FirstOrDefaultAsync(u => u.EmailLogin == request.Email);

        if (usuario == null || usuario.IdEstadoNavigation.Descripcion != "ACTIVO")
            return Ok(true);

        var codigo = GenerarCodigoConfirmacion();
        usuario.CodigoConfirmacion = codigo;
        usuario.FechaEnvioCodigo = DateTime.UtcNow;

        PrepareAuditableEntity(usuario, isNew: false);
        await _identityContext.SaveChangesAsync();

        await _emailService.SendEmailAsync(
            request.Email,
            "Código de recuperación — OWEN",
            EmailTemplateHelper.GetCodigoVerificationEmail(
                "Recuperación de contraseña",
                "Recibimos una solicitud para restablecer tu contraseña. Tu código de verificación es:",
                codigo)
        );

        return Ok(true);
    }

    public async Task<OperationResponse<bool>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdEstadoNavigation)
            .FirstOrDefaultAsync(u => u.EmailLogin == request.Email);

        if (usuario == null)
            return BadRequest<bool>("Solicitud de recuperación inválida.");

        if (usuario.IdEstadoNavigation.Descripcion != "ACTIVO")
            return BadRequest<bool>("La cuenta no está activa.");

        if (usuario.CodigoConfirmacion != request.Codigo)
            return BadRequest<bool>("El código de recuperación es incorrecto.");

        if (usuario.FechaEnvioCodigo.HasValue &&
            DateTime.UtcNow > usuario.FechaEnvioCodigo.Value.AddMinutes(30))
            return BadRequest<bool>("El código expiró. Solicitá uno nuevo.");

        usuario.PasswordHash = BC.HashPassword(request.NuevaPassword);
        usuario.CodigoConfirmacion = null;
        usuario.FechaEnvioCodigo = null;

        PrepareAuditableEntity(usuario, isNew: false);
        await _identityContext.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RESET_PASSWORD", "IAM",
            new { Mensaje = $"El usuario {usuario.EmailLogin} restableció su contraseña." });

        return Ok(true);
    }

    private bool ShouldExposeDevEmailCode()
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? _configuration["DOTNET_ENVIRONMENT"];
        var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        var hasEmailConfig = !string.IsNullOrWhiteSpace(_configuration["Resend:From"])
                             && !string.IsNullOrWhiteSpace(_configuration["Resend:ApiKey"]);

        return isDevelopment && !hasEmailConfig;
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
                "Nuevo usuario pendiente de aprobación — OWEN",
                EmailTemplateHelper.GetAlertaNuevoUsuarioEmail($"{usuario.IdPersonaNavigation?.Nombre} {usuario.IdPersonaNavigation?.Apellido}", usuario.EmailLogin)
            );
        }
    }
}