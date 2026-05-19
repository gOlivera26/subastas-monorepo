namespace PortalSubastas.Providers.Application.AutoMapper;

public class ProviderProfile : Profile
{
    public ProviderProfile()
    {
        CreateMap<TProveedore, ProviderResponseDto>();
        CreateMap<TProveedore, ProviderListDto>()
            .ForMember(dest => dest.TipoPersona, opt => opt.MapFrom(src => src.IdTipoPersonaNavigation.Descripcion))
            .ForMember(dest => dest.HasConstanciaAfip, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.UrlConstanciaAfip)))
            .ForMember(dest => dest.RubrosCount, opt => opt.MapFrom(src => src.TProveedoresRubros.Count(r => r.FecBaja == null)))
            .ForMember(dest => dest.DomiciliosCount, opt => opt.Ignore());
        CreateMap<CreateProviderDto, TProveedore>();
        CreateMap<UpdateProviderDto, TProveedore>();

        CreateMap<TRubro, RubroDto>();
        CreateMap<TRubro, ProviderRubroDto>()
            .ForMember(dest => dest.IdRubro, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Codigo, opt => opt.MapFrom(src => src.Codigo))
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion))
            .ForMember(dest => dest.IdRubroPadre, opt => opt.MapFrom(src => src.IdRubroPadre));
        CreateMap<TRubro, RubroListDto>()
            .ForMember(dest => dest.RubroPadre, opt => opt.MapFrom(src => src.IdRubroPadreNavigation != null ? src.IdRubroPadreNavigation.Descripcion : null))
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => src.FecBaja == null))
            .ForMember(dest => dest.Imputable, opt => opt.MapFrom(src => src.Imputable));
        CreateMap<CreateRubroDto, TRubro>();
        CreateMap<UpdateRubroDto, TRubro>();

        CreateMap<TDomicilio, DomicilioDto>()
            .ForMember(dest => dest.TipoDomicilio, opt => opt.MapFrom(src => src.IdTipoDomicilioNavigation.Descripcion))
            .ForMember(dest => dest.Provincia, opt => opt.MapFrom(src => src.IdProvinciaNavigation.Nombre));
        CreateMap<CreateDomicilioDto, TDomicilio>();
        CreateMap<UpdateDomicilioDto, TDomicilio>();

        CreateMap<TTipoDomicilio, TipoDomicilioDto>();
        CreateMap<TProvincia, ProvinciaDto>();
    }
}
