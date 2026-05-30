using AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.AutoMapper;

public class CotizacionProfile : Profile
{
    public CotizacionProfile()
    {
        CreateMap<TCotizacion, CotizacionResponseDto>();
        CreateMap<CotizacionRequestDto, TCotizacion>();

        CreateMap<TCotizacionEspecificacion, CotizacionEspecificacionResponseDto>();
        CreateMap<CotizacionEspecificacionRequestDto, TCotizacionEspecificacion>();

        CreateMap<TCotizacionDetalle, CotizacionDetalleResponseDto>();
        CreateMap<CotizacionDetalleRequestDto, TCotizacionDetalle>();

        CreateMap<TCotizacionRenglon, CotizacionRenglonResponseDto>();
        CreateMap<CotizacionRenglonRequestDto, TCotizacionRenglon>();
        CreateMap<TCotizacionProveedor, CotizacionProveedorResponseDto>();
    }
}
