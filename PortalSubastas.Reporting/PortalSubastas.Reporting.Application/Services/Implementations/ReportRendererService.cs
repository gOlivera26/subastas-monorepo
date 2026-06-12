using PortalSubastas.Reporting.Application.Services.Interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PortalSubastas.Reporting.Application.Services.Implementations;

public sealed class ReportRendererService : IReportRendererService
{
    private static IBrowser? _browser;
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);

    public async Task<byte[]> RenderPdfFromHtmlAsync(
        string html,
        ReporteMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        await InitBrowserAsync(cancellationToken);

        if (_browser is null)
        {
            throw new InvalidOperationException("No se pudo inicializar Chromium para generar el PDF.");
        }

        await using var page = await _browser.NewPageAsync();
        await page.SetContentAsync(html);
        await page.EmulateMediaTypeAsync(MediaType.Print);

        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            DisplayHeaderFooter = true,
            HeaderTemplate = $"""
                <div style="font-size:8px;color:#6b7280;width:100%;padding:0 10mm;">
                    {System.Net.WebUtility.HtmlEncode(metadata.Title)}
                </div>
                """,
            FooterTemplate = """
                <div style="font-size:8px;color:#6b7280;width:100%;padding:0 10mm;text-align:right;">
                    <span class="pageNumber"></span> / <span class="totalPages"></span>
                </div>
                """,
            MarginOptions = new MarginOptions
            {
                Top = "18mm",
                Bottom = "18mm",
                Left = "10mm",
                Right = "10mm"
            }
        });
    }

    private static async Task InitBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            return;
        }

        await BrowserLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is not null)
            {
                return;
            }

            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox"]
            });
        }
        finally
        {
            BrowserLock.Release();
        }
    }
}
