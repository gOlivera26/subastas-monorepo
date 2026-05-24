namespace PortalSubastas.Licitaciones.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected IActionResult Return<T>(OperationResponse<T> response)
    {
        var code = response.Code ?? (response.Success == true ? 200 : 500);

        return code switch
        {
            200 => Ok(response),
            201 => Created("", response),
            400 => BadRequest(response),
            401 => Unauthorized(response),
            403 => Forbid(response.Message ?? "Forbidden"),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => StatusCode(code, response)
        };
    }
}
