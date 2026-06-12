using PortalSubastas.Licitaciones.Application.RequestDto.Proveedor;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IProveedorService
{
    Task<OperationResponse<object>> AddProveedorAsync(int idCotizacion, ProveedorAddDto dto);
}
