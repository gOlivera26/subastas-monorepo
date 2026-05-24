using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.AutoMapper;

public class CatalogosProfile : Profile
{
    public CatalogosProfile()
    {
        CreateMap<TCatalogosBien, CatalogoBienResponseDto>()
            .ForMember(dest => dest.NumeroJurisdiccion, opt => opt.MapFrom(src => src.IdOrganizacion))
            .ForMember(dest => dest.IdCategoriaBien, opt => opt.MapFrom(src => src.IdObjetoGasto));

        CreateMap<TMonedum, MonedaResponseDto>();

        CreateMap<TCategoriasProgramatica, CategoriaProgramaticaResponseDto>()
            .ForMember(dest => dest.Codigo, opt => opt.MapFrom(src => src.Codigo.ToString()));

        CreateMap<TObjetosGasto, ObjetoGastoResponseDto>();
    }
}
