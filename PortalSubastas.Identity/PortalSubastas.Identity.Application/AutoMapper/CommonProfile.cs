using PortalSubastas.Identity.Application.ResponseDto.UnidadAdministrativa;
using PortalSubastas.Identity.Application.RequestDto.UnidadAdministrativa;
using PortalSubastas.Identity.Application.ResponseDto.Vigencia;
using PortalSubastas.Identity.Application.RequestDto.Organizacion;
using PortalSubastas.Identity.Application.RequestDto.Vigencia;
using PortalSubastas.Identity.Application.ResponseDto.ObjetoGasto;
using PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;
using PortalSubastas.Identity.Application.ResponseDto.CatalogoBien;
using PortalSubastas.Identity.Application.RequestDto.CatalogoBien;
using PortalSubastas.Identity.Application.ResponseDto.CategoriaProgramatica;
using PortalSubastas.Identity.Application.RequestDto.CategoriaProgramatica;
using PortalSubastas.Identity.Application.ResponseDto.Moneda;
using PortalSubastas.Identity.Application.RequestDto.Moneda;
using PortalSubastas.Identity.Application.ResponseDto.SubResponsable;
using PortalSubastas.Identity.Application.RequestDto.SubResponsable;
using PortalSubastas.Identity.Application.ResponseDto.ObjetoGasto;
using PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;
using PortalSubastas.Identity.Application.RequestDto.Role;

namespace PortalSubastas.Identity.Application.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<TRole, RoleResponseDto>()
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion ?? string.Empty));
        CreateMap<RoleRequestDto, TRole>();

        CreateMap<TOrganizacione, OrganizationResponseDto>();
        CreateMap<OrganizationRequestDto, TOrganizacione>();

        CreateMap<TVigencia, VigenciaResponseDto>();
        CreateMap<VigenciaRequestDto, TVigencia>();

        CreateMap<TUnidadAdministrativa, UnidadAdministrativaResponseDto>()
            .ForMember(dest => dest.VigenciaEjercicio, opt => opt.MapFrom(src => src.IdVigenciaNavigation != null ? src.IdVigenciaNavigation.Ejercicio.ToString() : null))
            .ForMember(dest => dest.OrganizacionNombre, opt => opt.MapFrom(src => src.IdOrganizacionNavigation != null ? src.IdOrganizacionNavigation.Nombre : null));
        CreateMap<UnidadAdministrativaRequestDto, TUnidadAdministrativa>();

        CreateMap<TObjetoGasto, ObjetoGastoResponseDto>()
            .ForMember(dest => dest.VigenciaEjercicio, opt => opt.MapFrom(src => src.IdVigenciaNavigation != null ? src.IdVigenciaNavigation.Ejercicio.ToString() : null))
            .ForMember(dest => dest.OrganizacionNombre, opt => opt.MapFrom(src => src.IdOrganizacionNavigation != null ? src.IdOrganizacionNavigation.Nombre : null));
        CreateMap<ObjetoGastoRequestDto, TObjetoGasto>();

        CreateMap<TCatalogoBien, CatalogoBienResponseDto>()
            .ForMember(dest => dest.VigenciaEjercicio, opt => opt.MapFrom(src => src.IdVigenciaNavigation != null ? src.IdVigenciaNavigation.Ejercicio.ToString() : null))
            .ForMember(dest => dest.OrganizacionNombre, opt => opt.MapFrom(src => src.IdOrganizacionNavigation != null ? src.IdOrganizacionNavigation.Nombre : null))
            .ForMember(dest => dest.ObjetoGastoNombre, opt => opt.MapFrom(src => src.IdObjetoGastoNavigation != null ? src.IdObjetoGastoNavigation.NombreObjeto : null));
        CreateMap<CatalogoBienRequestDto, TCatalogoBien>();

        CreateMap<TCategoriaProgramatica, CategoriaProgramaticaResponseDto>()
            .ForMember(d => d.VigenciaEjercicio, o => o.MapFrom(s => s.IdVigenciaNavigation != null ? s.IdVigenciaNavigation.Ejercicio.ToString() : null))
            .ForMember(d => d.OrganizacionNombre, o => o.MapFrom(s => s.IdOrganizacionNavigation != null ? s.IdOrganizacionNavigation.Nombre : null))
            .ForMember(d => d.UnidadAdmNombre, o => o.MapFrom(s => s.IdUnidadAdmNavigation != null ? s.IdUnidadAdmNavigation.NombreUnidadAdm : null));
        CreateMap<CategoriaProgramaticaRequestDto, TCategoriaProgramatica>();

        CreateMap<TMoneda, MonedaResponseDto>();
        CreateMap<MonedaRequestDto, TMoneda>();

        CreateMap<TSubResponsable, SubResponsableResponseDto>();
        CreateMap<SubResponsableRequestDto, TSubResponsable>();
    }
}
