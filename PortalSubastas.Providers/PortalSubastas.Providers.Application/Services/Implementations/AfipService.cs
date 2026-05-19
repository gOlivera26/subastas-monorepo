namespace PortalSubastas.Providers.Application.Services.Implementations;

/// <summary>
/// Servicio de verificación de CUIT contra el Padrón de ARCA (ex AFIP).
/// IMPLEMENTACIÓN ACTUAL: Mock — devuelve datos ficticios.
///
/// TODO — Pasos para integración real con ARCA:
///
/// 1. OBTENER CERTIFICADO DIGITAL (.pfx)
///    - Ir a afip.gob.ar con clave fiscal → WSASS → "Nuevo Certificado"
///    - Asociar el servicio "ws_sr_constancia_inscripcion"
///    - Descargar el archivo .pfx (PKCS#12)
///    - Para producción: usar "Administrador de Certificados Digitales"
///
/// 2. CONFIGURAR appsettings.json en Providers.API:
///    "Afip": {
///      "CuitRepresentada": "20999999993",
///      "CertificadoPfxPath": "/secrets/afip-cert.pfx",
///      "CertificadoPassword": "",
///      "Produccion": false
///    }
///
/// 3. IMPLEMENTAR AfipCuitService (código ya generado por Claude):
///    - Flujo completo: WSAA (obtener TA) → ws_sr_constancia_inscripcion (getPersona_v2)
///    - Caché automático del Ticket de Acceso (válido ~12hs)
///    - SOAP over HTTPS, firma CMS/PKCS#7 con certificado X.509
///    - Endpoints:
///      WSAA Homo:  https://wsaahomo.afip.gov.ar/ws/services/LoginCms
///      WSAA Prod:  https://wsaa.afip.gov.ar/ws/services/LoginCms
///      Padrón Homo: https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5
///      Padrón Prod: https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5
///    - Manual oficial: https://www.afip.gob.ar/ws/WSCI/manual_ws_sr_ws_constancia_inscripcion.pdf
///
/// 4. REGISTRAR EN DI (Program.cs de Providers.API):
///    builder.Services.AddAfipCuitService(opts => {
///        opts.CuitRepresentada   = config["Afip:CuitRepresentada"];
///        opts.CertificadoPfxPath = config["Afip:CertificadoPfxPath"];
///        opts.CertificadoPassword = config["Afip:CertificadoPassword"] ?? "";
///        opts.Produccion         = config.GetValue<bool>("Afip:Produccion");
///    });
///
/// 5. REEMPLAZAR ESTA CLASE por la implementación real que implemente IAfipService
///
/// DATOS QUE DEVUELVE EL SERVICIO REAL (getPersona_v2):
///    - tipoPersona: "FISICA" o "JURIDICA"
///    - razonSocial (jurídicas) / apellido + nombre (físicas)
///    - estadoClave: "ACTIVO", "INACTIVO", "BLOQUEADO"
///    - domicilioFiscal: dirección, localidad, CP, provincia
///    - impuestos y regímenes inscriptos, actividades SUPA
/// </summary>
public class AfipService : IAfipService
{
    public async Task<OperationResponse<AfipPersonDataDto>> GetPersonDataAsync(string cuit)
    {
        if (!IsValidCuit(cuit))
            return OperationResponse<AfipPersonDataDto>.CustomErrorResponse(400, "El CUIT ingresado no tiene un formato valido.");

        // TODO: Reemplazar mock por llamada real a ARCA:
        //   var persona = await _afipCuitService.ConsultarCuitAsync(long.Parse(cleanCuit));
        //   if (persona is null) return NotFound("CUIT no encontrado en el Padrón de ARCA");
        //   return Ok(new AfipPersonDataDto { ... });

        await Task.Delay(500);

        return OperationResponse<AfipPersonDataDto>.SuccessResponse(new AfipPersonDataDto
        {
            Nombre = $"Empresa CUIT {cuit}",
            TipoPersona = "JURIDICA",
            DomicilioFiscal = "Domicilio fiscal AFIP",
            EstadoClave = "ACTIVO"
        });
    }

    private static bool IsValidCuit(string cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit)) return false;
        var clean = cuit.Replace("-", "").Replace(".", "");
        return Regex.IsMatch(clean, @"^\d{11}$");
    }
}
