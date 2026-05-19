namespace PortalSubastas.Identity.Application.AutoMapper;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<TUsuario, PendingUserDto>()
            .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.NombreCompleto, opt =>
                opt.MapFrom(src => $"{src.IdPersonaNavigation.Nombre} {src.IdPersonaNavigation.Apellido}"))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EmailLogin))
            .ForMember(dest => dest.Documento, opt => opt.MapFrom(src => src.IdPersonaNavigation.NroDocumento))
            .ForMember(dest => dest.FechaRegistro, opt => opt.MapFrom(src => src.FecIng))
            .ForMember(dest => dest.TipoUsuario, opt => opt.MapFrom(src => DetermineTipoUsuario(src)))
            .ForMember(dest => dest.EntidadRepresentada, opt => opt.MapFrom(src => DetermineEntidad(src)));

        CreateMap<TUsuario, ActiveUserDto>()
            .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.NombreCompleto, opt =>
                opt.MapFrom(src => $"{src.IdPersonaNavigation.Nombre} {src.IdPersonaNavigation.Apellido}"))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EmailLogin))
            .ForMember(dest => dest.Documento, opt => opt.MapFrom(src => src.IdPersonaNavigation.NroDocumento))
            .ForMember(dest => dest.Rol, opt => opt.MapFrom(src => src.IdRolNavigation.Nombre))
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.IdEstadoNavigation.Descripcion))
            .ForMember(dest => dest.TipoUsuario, opt => opt.MapFrom(src => DetermineTipoUsuario(src)))
            .ForMember(dest => dest.EntidadRepresentada, opt => opt.MapFrom(src => DetermineEntidad(src)));

        CreateMap<TUsuario, UserAuditDto>()
            .ForMember(dest => dest.CreadoPor, opt => opt.MapFrom(src => src.UsrIng))
            .ForMember(dest => dest.FechaRegistro, opt => opt.MapFrom(src => src.FecIng))
            .ForMember(dest => dest.FechaAprobacion, opt => opt.MapFrom(src => src.FechaAprobacion))
            .ForMember(dest => dest.FechaModificacion, opt => opt.MapFrom(src => src.FecMod))
            .ForMember(dest => dest.ModificadoPor, opt => opt.MapFrom(src => src.UsrMod))
            .ForMember(dest => dest.UltimoAcceso, opt => opt.MapFrom(src => src.UltimoAcceso))
            .ForMember(dest => dest.AprobadoPorNombre, opt => opt.MapFrom(src =>
                $"{src.AprobadoPorNavigation.IdPersonaNavigation.Nombre} {src.AprobadoPorNavigation.IdPersonaNavigation.Apellido}"));
    }

    private static string DetermineTipoUsuario(TUsuario u)
    {
        if (u.TJurisdiccionesUsuarios.Any()) return "Gestor Licitación";
        if (u.IdPersonaNavigation.TProveedoresRepresentantes.Any()) return "Proveedor Directo";
        return "Independiente";
    }

    private static string DetermineEntidad(TUsuario u)
    {
        if (u.TJurisdiccionesUsuarios.Any())
            return u.TJurisdiccionesUsuarios.First().IdOrganizacionNavigation.Nombre;

        if (u.IdPersonaNavigation.TProveedoresRepresentantes.Any())
            return u.IdPersonaNavigation.TProveedoresRepresentantes.First().IdProveedorNavigation.RazonSocial;

        return "-";
    }
}
