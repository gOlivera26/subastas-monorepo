namespace PortalSubastas.Identity.Domain.Models;

public partial class PortalSubastasContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<TEstadosUsuario>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TModulo>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TPersona>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TRole>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TRolesModulo>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TTiposDocumento>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TTiposPersona>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TUsuario>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TJurisdiccionesUsuario>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TProveedoresRepresentante>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TProveedore>().HasQueryFilter(e => e.FecBaja == null);
        modelBuilder.Entity<TProveedoresRubro>().HasQueryFilter(e => e.FecBaja == null);
    }
}