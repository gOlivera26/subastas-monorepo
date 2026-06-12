namespace PortalSubastas.Reporting.Application.ResponseDto;

public sealed record ReporteDocumentoResponseDto(
    string FileName,
    string ContentType,
    byte[] Content);
