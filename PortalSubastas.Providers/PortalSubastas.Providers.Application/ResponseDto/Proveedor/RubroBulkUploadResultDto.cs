namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class RubroBulkUploadResultDto
{
    public int Procesados { get; set; }
    public int Creados { get; set; }
    public int Actualizados { get; set; }
    public int Omitidos { get; set; }
    public int RelacionesActualizadas { get; set; }
    public List<string> Errores { get; set; } = new();
}
