using PortalSubastas.Identity.Application.RequestDto.Users;
using PortalSubastas.Identity.Application.ResponseDto.Users;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : BaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Vincula a un usuario con una entidad específica (Organización/Gestor o Proveedor).
    /// </summary>
    /// <param name="id">El identificador único (GUID) del usuario.</param>
    /// <param name="request">Objeto que contiene el tipo de entidad (GESTOR/PROVEEDOR) y el ID de dicha entidad.</param>
    /// <returns>Una respuesta HTTP que indica el éxito de la vinculación, o un error 400 si el usuario ya posee una entidad vinculada.</returns>
    [HttpPost("{id}/link")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkEntity(Guid id, [FromBody] LinkEntityRequestDto request)
    {
        var result = await _userService.LinkUserEntityAsync(id, request);
        return Return(result);
    }

    /// <summary>
    /// Obtiene una lista detallada de todos los usuarios que se encuentran en estado Pendiente de aprobación.
    /// </summary>
    /// <returns>Una respuesta HTTP que contiene una lista de objetos <see cref="PendingUserDto"/> con la información de los usuarios pendientes.</returns>
    [HttpGet("pending")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<PendingUserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<List<PendingUserDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingUsers()
    {
        var result = await _userService.GetPendingUsersAsync();
        return Return(result);
    }

    /// <summary>
    /// Aprueba la solicitud de un usuario pendiente, cambiando su estado a Activo.
    /// </summary>
    /// <remarks>Esta acción registra qué administrador realizó la aprobación y dispara un evento de auditoría.</remarks>
    /// <param name="id">El identificador único (GUID) del usuario a aprobar.</param>
    /// <returns>Una respuesta HTTP indicando el éxito de la operación, o error si el usuario no estaba en estado pendiente.</returns>
    [HttpPost("{id}/approve")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApproveUser(Guid id)
    {
        var result = await _userService.ApproveUserAsync(id);
        return Return(result);
    }

    /// <summary>
    /// Obtiene una lista paginada y filtrada de los usuarios activos en el sistema.
    /// </summary>
    /// <param name="page">El número de página actual (por defecto 1).</param>
    /// <param name="pageSize">La cantidad de registros a devolver por página (por defecto 10).</param>
    /// <param name="searchTerm">Término de búsqueda opcional para filtrar por nombre, apellido, documento o email.</param>
    /// <returns>Una respuesta HTTP que contiene la lista paginada de objetos <see cref="ActiveUserDto"/> y el total de registros encontrados.</returns>
    [HttpGet("active")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<ActiveUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "", [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _userService.GetActiveUsersAsync(page, pageSize, searchTerm, sortBy, sortDirection);
        return Return(result);
    }

    /// <summary>
    /// Genera una nueva contraseña temporal aleatoria para el usuario especificado.
    /// </summary>
    /// <param name="id">El identificador único (GUID) del usuario al que se le restablecerá la contraseña.</param>
    /// <returns>Una respuesta HTTP que contiene la nueva contraseña temporal generada en formato de texto plano.</returns>
    [HttpPost("{id}/reset-password")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var result = await _userService.ResetUserPasswordAsync(id);
        return Return(result);
    }

    /// <summary>
    /// Elimina cualquier vínculo que el usuario tenga actualmente con una Organización o Proveedor.
    /// </summary>
    /// <param name="id">El identificador único (GUID) del usuario a desvincular.</param>
    /// <returns>Una respuesta HTTP indicando el éxito de la operación, o un error si el usuario no poseía vinculaciones previas.</returns>
    [HttpPost("{id}/unlink")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkEntity(Guid id)
    {
        var result = await _userService.UnlinkUserEntityAsync(id);
        return Return(result);
    }

    /// <summary>
    /// Actualiza el rol de sistema asignado a un usuario específico.
    /// </summary>
    /// <param name="id">El identificador único (GUID) del usuario.</param>
    /// <param name="newRoleId">El ID numérico del nuevo rol que se desea asignar.</param>
    /// <returns>Una respuesta HTTP indicando el éxito de la actualización del rol.</returns>
    [HttpPut("{id}/role")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] int newRoleId)
    {
        var result = await _userService.UpdateUserRoleAsync(id, newRoleId);
        return Return(result);
    }

    /// <summary>
    /// Consulta la información detallada de auditoría de un usuario.
    /// </summary>
    /// <remarks>Incluye fechas de registro, último acceso, aprobación y el nombre del administrador responsable.</remarks>
    /// <param name="id">El identificador único (GUID) del usuario a consultar.</param>
    /// <returns>Una respuesta HTTP que contiene un objeto <see cref="UserAuditDto"/> con toda la trazabilidad del usuario.</returns>
    [HttpGet("{id}/audit")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<UserAuditDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAudit(Guid id)
    {
        var result = await _userService.GetUserAuditAsync(id);
        return Return(result);
    }
}