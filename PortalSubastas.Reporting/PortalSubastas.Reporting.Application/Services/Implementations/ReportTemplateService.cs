using PortalSubastas.Reporting.Application.Services.Interfaces;
using Razor.Templating.Core;

namespace PortalSubastas.Reporting.Application.Services.Implementations;

public sealed class ReportTemplateService : IReportTemplateService
{
    public Task<string> RenderLicitacionAsync(ReporteLicitacion model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/LicitacionDetalle.cshtml", model);
    }

    public Task<string> RenderActaPrelacionAsync(ActaPrelacionReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/ActaPrelacion.cshtml", model);
    }

    public Task<string> RenderDetalleSubastaAsync(DetalleSubastaReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/DetalleSubasta.cshtml", model);
    }

    public Task<string> RenderProveedoresInvitadosAsync(ProveedoresInvitadosReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/ProveedoresInvitados.cshtml", model);
    }

    public Task<string> RenderPreguntasRespuestasAsync(PreguntasRespuestasReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/PreguntasRespuestas.cshtml", model);
    }

    public Task<string> RenderDesistimientoAsync(DesistimientoReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/Desistimiento.cshtml", model);
    }

    public Task<string> RenderObservacionesProveedoresAsync(ObservacionesProveedoresReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/ObservacionesProveedores.cshtml", model);
    }

    public Task<string> RenderAuditoriaSubastaAsync(AuditoriaSubastaReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/AuditoriaSubasta.cshtml", model);
    }

    public Task<string> RenderVerificacionDocumentacionAsync(VerificacionDocumentacionReport model)
    {
        return RazorTemplateEngine.RenderAsync("~/Services/Reportes/Templates/VerificacionDocumentacion.cshtml", model);
    }
}
