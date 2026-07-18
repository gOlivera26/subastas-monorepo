namespace PortalSubastas.Identity.Application.Utilities;

public static class EmailTemplateHelper
{
    // Diseño base del contenedor (Estilo Light Mode / Email Worker)
    private const string BaseTemplate = @"
    <!DOCTYPE html>
    <html>
    <body style='margin:0;padding:0;background-color:#f3f4f6;font-family:""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;color:#111827;'>
        <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color:#f3f4f6;padding:40px 20px;'>
            <tr>
                <td align='center'>
                    <table width='100%' max-width='600' border='0' cellspacing='0' cellpadding='0' style='max-width:600px;background-color:#ffffff;border-radius:8px;border:1px solid #e5e7eb;overflow:hidden;box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                        <!-- Header -->
                        <tr>
                            <td align='center' style='padding:30px 40px;background-color:#0f172a;'>
                                <h2 style='margin:0;color:#ffffff;font-size:24px;letter-spacing:2px;font-weight:700;'>OWEN</h2>
                                <p style='margin:5px 0 0 0;color:#94a3b8;font-size:11px;letter-spacing:1px;text-transform:uppercase;'>Subastas Electrónicas</p>
                            </td>
                        </tr>
                        <!-- Accent Line -->
                        <tr>
                            <td style='height:4px;background-color:{ACCENT_COLOR};'></td>
                        </tr>
                        <!-- Body -->
                        <tr>
                            <td style='padding:40px;'>
                                {CONTENT}
                            </td>
                        </tr>
                        <!-- Footer -->
                        <tr>
                            <td align='center' style='padding:20px 40px 30px;background-color:#f9fafb;border-top:1px solid #e5e7eb;'>
                                <p style='margin:0;color:#6b7280;font-size:12px;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
                                <p style='margin:5px 0 0 0;color:#6b7280;font-size:12px;'>&copy; {YEAR} OWEN. Todos los derechos reservados.</p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </body>
    </html>";

    private static string BuildEmail(string content, string accentColor = "#02b8cc")
    {
        return BaseTemplate
            .Replace("{CONTENT}", content)
            .Replace("{ACCENT_COLOR}", accentColor)
            .Replace("{YEAR}", DateTime.Now.Year.ToString());
    }

    // 1 & 2 & 3. Plantilla para Códigos (Registro, Reenvío y Recuperación)
    public static string GetCodigoVerificationEmail(string titulo, string mensaje, string codigo)
    {
        string content = $@"
            <h2 style='margin:0 0 20px;color:#111827;font-size:22px;font-weight:600;'>{titulo}</h2>
            <p style='margin:0 0 30px;color:#4b5563;font-size:15px;line-height:1.6;'>{mensaje}</p>
            
            <div style='text-align:center;margin:30px 0;'>
                <span style='display:inline-block;background-color:#f9fafb;border:1px solid #d1d5db;border-radius:8px;padding:16px 32px;color:#0f172a;font-size:36px;font-family:monospace;font-weight:bold;letter-spacing:8px;'>{codigo}</span>
            </div>
            
            <p style='margin:0 0 10px;color:#4b5563;font-size:14px;'>Este código expira en <strong>30 minutos</strong>.</p>
            <p style='margin:0;color:#6b7280;font-size:13px;'>Si no solicitaste esta acción, podés ignorar este mensaje.</p>
        ";
        return BuildEmail(content, "#02b8cc"); // Usamos el cian por defecto
    }

    // 4. Plantilla de Cuenta Aprobada
    public static string GetCuentaAprobadaEmail(string loginUrl)
    {
        string content = $@"
            <h2 style='margin:0 0 20px;color:#111827;font-size:22px;font-weight:600;'>¡Bienvenido a OWEN!</h2>
            <p style='margin:0 0 30px;color:#4b5563;font-size:15px;line-height:1.6;'>Tu cuenta ha sido aprobada exitosamente. Ya podés ingresar al sistema utilizando tu correo electrónico y contraseña.</p>
            
            <div style='text-align:center;margin:35px 0;'>
                <a href='{loginUrl}' style='display:inline-block;background-color:#0f172a;color:#ffffff;font-size:14px;font-weight:600;text-decoration:none;padding:14px 32px;border-radius:6px;text-transform:uppercase;letter-spacing:0.5px;'>IR AL INICIO DE SESIÓN</a>
            </div>
        ";
        return BuildEmail(content, "#10b981"); // Verde de éxito (como en la imagen de respuestas)
    }

    // 5. Plantilla de Alerta a SUPERADMINs
    public static string GetAlertaNuevoUsuarioEmail(string nombreUsuario, string emailUsuario)
    {
        string content = $@"
            <h2 style='margin:0 0 20px;color:#111827;font-size:22px;font-weight:600;'>Nuevo usuario pendiente</h2>
            <p style='margin:0 0 20px;color:#4b5563;font-size:15px;line-height:1.6;'>Un nuevo usuario ha confirmado su correo y requiere aprobación para operar en la plataforma.</p>
            
            <table width='100%' border='0' cellspacing='0' cellpadding='15' style='background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;margin-bottom:30px;'>
                <tr>
                    <td style='color:#111827;font-size:15px;'><strong>Usuario:</strong> {nombreUsuario}</td>
                </tr>
                <tr>
                    <td style='color:#111827;font-size:15px;border-top:1px solid #e5e7eb;'><strong>Email:</strong> {emailUsuario}</td>
                </tr>
            </table>
            
            <p style='margin:0;color:#4b5563;font-size:15px;'>Ingresá al panel de administración (Sección: Aprobaciones) para revisar la solicitud.</p>
        ";
        return BuildEmail(content, "#f59e0b"); // Amarillo para indicar acción pendiente
    }
}