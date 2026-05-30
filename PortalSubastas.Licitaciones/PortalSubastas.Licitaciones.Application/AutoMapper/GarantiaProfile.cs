using PortalSubastas.Licitaciones.Application.ResponseDto.Garantia;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.AutoMapper;

public class GarantiaProfile : Profile
{
    public GarantiaProfile()
    {
        CreateMap<TGarantiaSubasta, GarantiaResponseDto>()
            .ForMember(d => d.FechaPagare, o => o.MapFrom(s => s.FechaPagare.HasValue ? s.FechaPagare.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null));
    }
}