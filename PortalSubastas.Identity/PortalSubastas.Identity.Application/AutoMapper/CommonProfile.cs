namespace PortalSubastas.Identity.Application.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<TRole, RoleResponseDto>()
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion ?? string.Empty));

        CreateMap<TOrganizacione, OrganizationResponseDto>();
    }
}
