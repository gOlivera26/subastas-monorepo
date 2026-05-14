namespace PortalSubastas.Identity.Application.AutoMapper;

public class AuthProfile : Profile
{
    public AuthProfile()
    {
        CreateMap<TUsuario, ProfileResponseDto>()
            .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.IdPersonaNavigation.Nombre))
            .ForMember(dest => dest.Apellido, opt => opt.MapFrom(src => src.IdPersonaNavigation.Apellido))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EmailLogin))
            .ForMember(dest => dest.NroDocumento, opt => opt.MapFrom(src => src.IdPersonaNavigation.NroDocumento))
            .ForMember(dest => dest.Telefono, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.IdPersonaNavigation.Telefono) ? "No especificado" : src.IdPersonaNavigation.Telefono))
            .ForMember(dest => dest.Rol, opt => opt.MapFrom(src => src.IdRolNavigation.Nombre));
    }
}
