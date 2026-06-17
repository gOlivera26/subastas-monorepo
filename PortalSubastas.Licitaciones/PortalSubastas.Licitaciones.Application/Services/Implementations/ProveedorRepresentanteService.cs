using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class ProveedorRepresentanteService : IProveedorRepresentanteService
{
    private readonly PortalSubastasContext _context;
    private readonly ILogger<ProveedorRepresentanteService> _logger;

    public ProveedorRepresentanteService(PortalSubastasContext context, ILogger<ProveedorRepresentanteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<(string Email, string NombrePersona)>> GetRepresentantesAsync(int idProveedor)
    {
        try
        {
            var representantes = await _context.TProveedoresRepresentantes
                .Include(r => r.IdPersonaNavigation)
                .Where(r => r.IdProveedor == idProveedor
                    && r.IdPersonaNavigation.EmailContacto != null
                    && r.IdPersonaNavigation.EmailContacto != "")
                .Select(r => new
                {
                    Email = r.IdPersonaNavigation.EmailContacto,
                    Nombre = r.IdPersonaNavigation.Nombre + " " + r.IdPersonaNavigation.Apellido
                })
                .ToListAsync();

            return representantes.Select(r => (r.Email, r.Nombre)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al consultar representantes del proveedor {IdProveedor}", idProveedor);
            return [];
        }
    }
}
