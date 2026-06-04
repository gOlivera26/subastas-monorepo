using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

public class DocumentoItemResponseDto
{
    public int IdDocItem { get; set; }
    public int IdCotizacion { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int IdProveedor { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string UrlArchivo { get; set; } = null!;
    public bool Enviado { get; set; }
    public DateTime? FechaCarga { get; set; }
}
