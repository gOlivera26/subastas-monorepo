using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Services;

public class SubastaCloserWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubastaCloserWorker> _logger;

    public SubastaCloserWorker(IServiceProvider serviceProvider, ILogger<SubastaCloserWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<PortalSubastasContext>();
                    var ahora = DateTime.Now;

                    // Buscamos subastas en curso (39) cuya fecha de finalización ya pasó
                    var subastasVencidas = await context.TCotizaciones
                        .Include(c => c.Especificacion)
                        .Where(c => c.IdEstado == 39 && c.Especificacion.FechaFinalizacionSubasta <= ahora)
                        .ToListAsync(stoppingToken);

                    if (subastasVencidas.Any())
                    {
                        foreach (var subasta in subastasVencidas)
                        {
                            subasta.IdEstado = 40; // 40 = Finalizada
                            subasta.UsrMod = "WORKER_CIERRE";
                            subasta.FecMod = ahora;
                        }
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"[WORKER] Se cerraron {subastasVencidas.Count} subastas automáticamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WORKER] Ocurrió un error al intentar cerrar subastas vencidas. Se reintentará en el próximo ciclo.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}