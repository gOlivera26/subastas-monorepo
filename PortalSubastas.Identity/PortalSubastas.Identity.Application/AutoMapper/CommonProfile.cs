using PortalSubastas.Identity.Application.ResponseDto.Organizacion;
using PortalSubastas.Identity.Application.ResponseDto.Proveedor;
using PortalSubastas.Identity.Application.ResponseDto.Role;

namespace PortalSubastas.Identity.Application.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<TRole, RoleResponseDto>()
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion ?? string.Empty));

        CreateMap<TOrganizacione, OrganizationResponseDto>();

        CreateMap<TProveedore, ProviderResponseDto>();
    }
}
