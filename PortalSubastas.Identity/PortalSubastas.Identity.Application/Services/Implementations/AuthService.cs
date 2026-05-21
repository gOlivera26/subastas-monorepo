namespace PortalSubastas.Identity.Application.Services.Implementations;

public class AuthService : BaseService, IAuthService
{
    private readonly PortalSubastasContext _identityContext;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuthService(
    PortalSubastasContext context,
    IConfiguration configuration,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache,
    IPublishEndpoint publishEndpoint)
    : base(context, mapper, httpContextAccessor, cache)
    {
        _identityContext = context;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var usuario = await _identityContext.TUsuarios
            .Include(u => u.IdPersonaNavigation)
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
            .Where(rm => rm.IdRol == usuario.IdRol)
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

        // Incluir módulos que tengan páginas asignadas aunque no estén en TRolesModulo
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

        // Merge sin duplicados por Id
        modulosPermitidos = modulosPermitidos
            .UnionBy(modulosDesdePaginas, m => m.Id)
            .OrderBy(m => modulosPermitidos.Concat(modulosDesdePaginas).ToList().IndexOf(m))
            .ToList();

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
                })
                .ToListAsync()
        };

        await PublishSystemLogAsync(_publishEndpoint, "INICIO_SESION", "IAM",
    new { Mensaje = $"El usuario {usuario.EmailLogin} inició sesión exitosamente." });

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

            var usuario = new TUsuario
            {
                IdPersona = persona.Id,
                IdRol = request.IdRol,
                IdEstado = 4,
                EmailLogin = request.Email,
                PasswordHash = BC.HashPassword(request.Password)
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

    private string GenerarJwtToken(TUsuario usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

        var organizacionPrincipal = _identityContext.TJurisdiccionesUsuarios
            .FirstOrDefault(j => j.IdUsuario == usuario.Id && j.EsPrincipal == true);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.EmailLogin),
            new(ClaimTypes.Name, $"{usuario.IdPersonaNavigation.Nombre} {usuario.IdPersonaNavigation.Apellido}"),
            new(ClaimTypes.Role, usuario.IdRolNavigation.Nombre)
        };

        if (organizacionPrincipal != null)
            claims.Add(new Claim("IdOrganizacion", organizacionPrincipal.IdOrganizacion.ToString()));

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
}
