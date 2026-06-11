namespace PortalSubastas.Reporting.Application.ResponseDto;

public sealed record ReporteCatalogoResponseDto(
    string Codigo,
    string Nombre,
    string Descripcion,
    string[] FormatosDisponibles);
