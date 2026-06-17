namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

/// <summary>
/// Consulta representantes activos de un proveedor con email configurado.
/// Reemplaza los SQL raw duplicados en controllers y services.
/// </summary>
public interface IProveedorRepresentanteService
{
    Task<List<(string Email, string NombrePersona)>> GetRepresentantesAsync(int idProveedor);
}
