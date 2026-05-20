using PortalSubastas.Identity.Application.RequestDto.Organizacion;
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

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(OperationResponse<List<OrganizationResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _organizationService.GetAllAsync();
            return Return(result);
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

        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(OperationResponse<OrganizationResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _organizationService.GetByIdAsync(id);
            return Return(result);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(OperationResponse<OrganizationResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] OrganizationRequestDto dto)
        {
            var result = await _organizationService.CreateAsync(dto);
            return Return(result);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(OperationResponse<OrganizationResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(int id, [FromBody] OrganizationRequestDto dto)
        {
            var result = await _organizationService.UpdateAsync(id, dto);
            return Return(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _organizationService.DeleteAsync(id);
            return Return(result);
        }
    }
}
