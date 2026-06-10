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
            .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(src => $"{src.IdPersonaNavigation.Nombre} {src.IdPersonaNavigation.Apellido}"))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EmailLogin))
            .ForMember(dest => dest.Documento, opt => opt.MapFrom(src => src.IdPersonaNavigation.NroDocumento))
            .ForMember(dest => dest.Rol, opt => opt.MapFrom(src => src.IdRolNavigation.Nombre))
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.IdEstadoNavigation.Descripcion))
            .ForMember(dest => dest.TipoUsuario, opt => opt.MapFrom(src => DetermineTipoUsuario(src)))
            .ForMember(dest => dest.EntidadRepresentada, opt => opt.MapFrom(src => DetermineEntidad(src)))
            .ForMember(dest => dest.Entidades, opt => opt.MapFrom(src => ObtenerListaEntidades(src)));

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
        var entidades = new List<string>();

        if (u.TJurisdiccionesUsuarios != null && u.TJurisdiccionesUsuarios.Any(j => j.FecBaja == null))
            entidades.AddRange(u.TJurisdiccionesUsuarios.Where(j => j.FecBaja == null).Select(j => j.IdOrganizacionNavigation.Nombre));

        if (u.IdPersonaNavigation?.TProveedoresRepresentantes != null && u.IdPersonaNavigation.TProveedoresRepresentantes.Any(p => p.FecBaja == null))
            entidades.AddRange(u.IdPersonaNavigation.TProveedoresRepresentantes.Where(p => p.FecBaja == null).Select(p => p.IdProveedorNavigation.RazonSocial));

        return entidades.Any() ? string.Join(" | ", entidades) : "-";
    }

    private static List<UserEntidadDto> ObtenerListaEntidades(TUsuario u)
    {
        var lista = new List<UserEntidadDto>();
        if (u.TJurisdiccionesUsuarios != null)
        {
            lista.AddRange(u.TJurisdiccionesUsuarios.Where(j => j.FecBaja == null).Select(j =>
                new UserEntidadDto { IdEntidad = j.IdOrganizacion, Tipo = "GESTOR", Nombre = j.IdOrganizacionNavigation.Nombre }));
        }
        if (u.IdPersonaNavigation?.TProveedoresRepresentantes != null)
        {
            lista.AddRange(u.IdPersonaNavigation.TProveedoresRepresentantes.Where(p => p.FecBaja == null).Select(p =>
                new UserEntidadDto { IdEntidad = p.IdProveedor, Tipo = "PROVEEDOR", Nombre = p.IdProveedorNavigation.RazonSocial }));
        }
        return lista;
    }
}
