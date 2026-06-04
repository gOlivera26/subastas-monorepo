using Microsoft.EntityFrameworkCore;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class PortalSubastasContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Filtros globales de soft delete (excluye registros con FecBaja != null)
        modelBuilder.Entity<TReserva>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TReservaDetalle>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TEstado>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TVigencia>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TUnidadesAdministrativa>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TSubResponsable>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TCategoriasProgramatica>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TCatalogosBien>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TObjetosGasto>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TGarantiaSubasta>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TCotizacionDocumento>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TDocumentoItemProveedor>().HasQueryFilter(e => e.FecBaja == null);
    }
}
