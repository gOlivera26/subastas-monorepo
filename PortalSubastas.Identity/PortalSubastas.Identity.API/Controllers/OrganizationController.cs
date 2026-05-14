using PortalSubastas.Identity.Application.ResponseDto.Organizacion;

namespace PortalSubastas.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationController : BaseController
    {
        private readonly IOrganizationService _organizationService;

        public OrganizationController(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        /// <summary>
        /// Obtiene la lista de organizaciones activas para el formulario de registro.
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous] 
        [ProducesResponseType(typeof(OperationResponse<List<OrganizationResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveOrganizations()
        {
            var result = await _organizationService.GetAllActiveAsync();
            return Return(result);
        }
    }
}
