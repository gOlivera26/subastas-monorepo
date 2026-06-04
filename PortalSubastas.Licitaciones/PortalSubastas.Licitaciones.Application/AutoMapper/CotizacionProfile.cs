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

        CreateMap<TMensaje, ConsultaResponseDto>()
            .ForMember(d => d.UsuarioPregunta, o => o.MapFrom(s => s.Usuario))
            .ForMember(d => d.Pregunta, o => o.MapFrom(s => s.Contenido))
            .ForMember(d => d.FechaPregunta, o => o.MapFrom(s => s.FecIng));

        CreateMap<TCotizacionDocumento, CotizacionDocumentoResponseDto>();

        CreateMap<TDocumentoItemProveedor, DocumentoItemResponseDto>()
            .ForMember(d => d.FechaCarga, o => o.MapFrom(s => s.FecIng));
    }
}
