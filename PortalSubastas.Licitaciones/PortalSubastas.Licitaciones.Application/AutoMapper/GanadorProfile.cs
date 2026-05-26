using AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.Ganador;
using PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.AutoMapper;

public class GanadorProfile : Profile
{
    public GanadorProfile()
    {
        CreateMap<TGanador, GanadorResponseDto>()
            .ForMember(d => d.NroCotizacion, o => o.MapFrom(s => s.IdCotizacionNavigation != null ? s.IdCotizacionNavigation.NroCotizacion : null));
        CreateMap<GanadorRequestDto, TGanador>();
    }
}
