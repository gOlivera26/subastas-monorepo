using PortalSubastas.Identity.Application.RequestDto.Login;
using PortalSubastas.Identity.Application.ResponseDto.Perfil;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Inicia sesión con email y contraseña, devuelve un token JWT si las credenciales son correctas.
    /// </summary>
    /// <param name="request">Objeto que contiene el email y la contraseña del usuario.</param>
    /// <returns>Una respuesta HTTP que contiene un objeto <see cref="OperationResponse{LoginResponseDto}"/> con los datos del usuario y el token JWT si las credenciales son correctas; de lo contrario, una respuesta de error 401.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Return(OperationResponse<LoginResponseDto>.BadRequestResponse("El email y contraseña son obligatorios."));
        }

        var result = await _authService.LoginAsync(request);

        return Return(result);
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="request">Objeto que contiene los datos requeridos para registrar una nueva cuenta de usuario y asociarla a una entidad si corresponde.</param>
    /// <returns>Una respuesta HTTP que contiene un objeto <see cref="OperationResponse{LoginResponseDto}"/> con los datos básicos del usuario creado; o un error 400 si las validaciones no se cumplen.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Return(OperationResponse<LoginResponseDto>.BadRequestResponse("El email y contraseña son obligatorios."));
        }

        var result = await _authService.RegisterAsync(request);

        return Return(result);
    }

    /// <summary>
    /// Obtiene la información de perfil del usuario autenticado.
    /// </summary>
    /// <remarks>El usuario debe estar autenticado para acceder a este método. Devuelve un código de estado
    /// HTTP 200 si la operación es exitosa o 401 si el usuario no está autorizado.</remarks>
    /// <returns>Una respuesta HTTP que contiene un objeto <see cref="OperationResponse{ProfileResponseDto}"/> con los datos del perfil del usuario si la solicitud es autorizada; de lo contrario, una respuesta de error 401 de autorización.</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<ProfileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<ProfileResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _authService.GetProfileAsync();
        return Return(result);
    }

    /// <summary>
    /// Permite a un usuario autenticado modificar su contraseña actual por una nueva.
    /// </summary>
    /// <param name="request">Objeto que contiene la contraseña actual para validación y la nueva contraseña elegida.</param>
    /// <returns>Una respuesta HTTP que contiene un objeto <see cref="OperationResponse{bool}"/> indicando el éxito de la operación; o un error 400 si las contraseñas están vacías o son idénticas.</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.PasswordActual) || string.IsNullOrWhiteSpace(request.NuevaPassword))
        {
            return Return(OperationResponse<bool>.BadRequestResponse("Las contraseñas no pueden estar vacías."));
        }

        if (request.PasswordActual == request.NuevaPassword)
        {
            return Return(OperationResponse<bool>.BadRequestResponse("La nueva contraseña no puede ser igual a la anterior."));
        }

        var result = await _authService.ChangePasswordAsync(request);
        return Return(result);
    }

    /// <summary>
    /// Actualiza los datos personales básicos del perfil del usuario autenticado.
    /// </summary>
    /// <param name="request">Objeto que contiene el nombre, apellido y teléfono a actualizar.</param>
    /// <returns>Una respuesta HTTP que contiene un objeto <see cref="OperationResponse{ProfileResponseDto}"/> con el perfil actualizado.</returns>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<ProfileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<ProfileResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var result = await _authService.UpdateProfileAsync(request);
        return Return(result);
    }

    /// <summary>
    /// Cambia el contexto activo del usuario y emite un nuevo JWT.
    /// </summary>
    [HttpPost("switch-context")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SwitchContext([FromBody] SwitchContextRequestDto request)
    {
        var result = await _authService.SwitchContextAsync(request);
        return Return(result);
    }

    /// <summary>
    /// Confirma el correo electrónico con el código de 6 dígitos recibido por email.
    /// </summary>
    [HttpPost("confirmar-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmarEmail([FromBody] ConfirmEmailRequestDto request)
    {
        var result = await _authService.ConfirmarEmailAsync(request);
        return Return(result);
    }

    /// <summary>
    /// Reenvía el código de confirmación al email del usuario.
    /// </summary>
    [HttpPost("reenviar-codigo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReenviarCodigo([FromBody] ReenviarCodigoRequestDto request)
    {
        var result = await _authService.ReenviarCodigoAsync(request.Email);
        return Return(result);
    }

    /// <summary>
    /// Envía un código de 6 dígitos al email para restablecer la contraseña.
    /// </summary>
    [HttpPost("solicitar-reset")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SolicitarReset([FromBody] SolicitarResetRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Return(OperationResponse<bool>.BadRequestResponse("El email es obligatorio."));

        var result = await _authService.SolicitarResetPasswordAsync(request);
        return Return(result);
    }

    /// <summary>
    /// Valida el código de recuperación y cambia la contraseña.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.NuevaPassword))
            return Return(OperationResponse<bool>.BadRequestResponse("Todos los campos son obligatorios."));

        if (request.NuevaPassword.Length < 6)
            return Return(OperationResponse<bool>.BadRequestResponse("La contraseña debe tener al menos 6 caracteres."));

        var result = await _authService.ResetPasswordAsync(request);
        return Return(result);
    }
}