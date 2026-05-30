using PortalSubastas.Licitaciones.Application.RequestDto.Reserva;
using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reserva;
using PortalSubastas.Licitaciones.Application.ResponseDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.AutoMapper;

public class ReservaProfile : Profile
{
    public ReservaProfile()
    {
        // Reserva Mappings
        CreateMap<TReserva, ReservaResponseDto>()
            .ForMember(dest => dest.DescripcionEstado, opt => opt.MapFrom(src => src.IdEstadoNavigation != null ? src.IdEstadoNavigation.Descripcion : null))
            .ForMember(dest => dest.NombreUnidadAdm, opt => opt.MapFrom(src => src.IdUnidadAdmNavigation != null ? src.IdUnidadAdmNavigation.NombreUnidadAdm : null))
            .ForMember(dest => dest.NombreSubResponsable, opt => opt.MapFrom(src => src.IdSubResponsableNavigation != null ? src.IdSubResponsableNavigation.Nombre : null));
        
        CreateMap<ReservaRequestDto, TReserva>();

        // ReservaDetalle Mappings
        CreateMap<TReservaDetalle, ReservaDetalleResponseDto>()
            .ForMember(dest => dest.NombreCategoriaProgramatica, opt => opt.MapFrom(src => src.IdCatProgNavigation != null ? src.IdCatProgNavigation.Nombre : null))
            .ForMember(dest => dest.NombreBien, opt => opt.MapFrom(src => src.IdItemNavigation != null ? src.IdItemNavigation.NItem : null))
            .ForMember(dest => dest.NombreMoneda, opt => opt.MapFrom(src => src.IdMonedaNavigation != null ? src.IdMonedaNavigation.Nombre : null))
            .ForMember(dest => dest.NombreObjetoGasto, opt => opt.MapFrom(src => src.IdObjetoGastoNavigation != null ? src.IdObjetoGastoNavigation.NombreObjeto : null))
            .ForMember(dest => dest.DescripcionEstado, opt => opt.MapFrom(src => src.IdEstadoNavigation != null ? src.IdEstadoNavigation.Descripcion : null));
        
        CreateMap<ReservaDetalleRequestDto, TReservaDetalle>();
    }
}
