using PortalSubastas.Identity.Application.ResponseDto.Organizacion;
using PortalSubastas.Identity.Application.ResponseDto.Proveedor;
using PortalSubastas.Identity.Application.ResponseDto.Role;
using PortalSubastas.Identity.Application.ResponseDto.Vigencia;
using PortalSubastas.Identity.Application.ResponseDto.UnidadAdministrativa;
using PortalSubastas.Identity.Application.RequestDto.Organizacion;
using PortalSubastas.Identity.Application.RequestDto.Vigencia;
using PortalSubastas.Identity.Application.RequestDto.UnidadAdministrativa;

namespace PortalSubastas.Identity.Application.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<TRole, RoleResponseDto>()
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion ?? string.Empty));

        CreateMap<TOrganizacione, OrganizationResponseDto>();
        CreateMap<OrganizationRequestDto, TOrganizacione>();

        CreateMap<TProveedore, ProviderResponseDto>();

        CreateMap<TVigencia, VigenciaResponseDto>();
        CreateMap<VigenciaRequestDto, TVigencia>();

        CreateMap<TUnidadAdministrativa, UnidadAdministrativaResponseDto>()
            .ForMember(dest => dest.VigenciaEjercicio, opt => opt.MapFrom(src => src.IdVigenciaNavigation != null ? src.IdVigenciaNavigation.Ejercicio.ToString() : null))
            .ForMember(dest => dest.OrganizacionNombre, opt => opt.MapFrom(src => src.IdOrganizacionNavigation != null ? src.IdOrganizacionNavigation.Nombre : null));
        CreateMap<UnidadAdministrativaRequestDto, TUnidadAdministrativa>();
    }
}
