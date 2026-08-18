using System.Security.Claims;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PortalSubastas.Licitaciones.Application.AutoMapper;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class CotizacionServiceReadVisibilityTests
{
    [Fact]
    public async Task GetDetalleReducidoAsync_AuthorizedProvider_ReturnsOnlySafeAuctionFields()
    {
        await using var context = CreateContext();
        context.TCotizaciones.AddRange(
            CreateAuction(1, 10, visibility: "1"),
            CreateAuction(2, 10, visibility: "1", deleted: true));
        await context.SaveChangesAsync();

        var service = CreateService(context, providerId: 7);
        var result = await service.GetDetalleReducidoAsync(1);
        var deletedResult = await service.GetDetalleReducidoAsync(2);

        result.Success.Should().BeTrue();
        result.Data!.Numero.Should().Be("2026/000001");
        result.Data.Expediente.Should().Be("EXP-1");
        result.Data.Objeto.Should().Be("Auction 1");
        typeof(SubastaDetalleReducidoDto).GetProperties().Select(p => p.Name)
            .Should().NotContain(["Proveedores", "IdOrganizacion", "IdProveedor"]);
        deletedResult.Code.Should().Be(404);
    }

    [Fact]
    public async Task GetResumenOfertasAsync_OwnAuction_ReturnsOnlyActiveAggregatesAndForeignAuctionIsNotFound()
    {
        await using var context = CreateContext();
        context.TCotizaciones.AddRange(
            CreateAuction(1, 10),
            CreateAuction(2, 20),
            CreateAuction(
                3,
                10,
                tipoContratacion: (int)TipoContratacion.SubastaDirecta,
                criterioAdjudicacion: 1),
            CreateAuction(4, 10, deleted: true));
        context.TOfertasSubastas.AddRange(
            new TOfertaSubasta { IdOfertaSubasta = 1, IdCotizacion = 1, IdProveedor = 7, IdCotizacionDetalle = 101, Monto = 120m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 2, IdCotizacion = 1, IdProveedor = 8, IdCotizacionDetalle = 101, Monto = 95m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 3, IdCotizacion = 1, IdProveedor = 9, IdCotizacionDetalle = 101, Monto = 80m, FechaOferta = DateTime.Now, FecBaja = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 4, IdCotizacion = 1, IdProveedor = 9, IdCotizacionDetalle = 102, Monto = 50m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 5, IdCotizacion = 1, IdProveedor = 9, IdCotizacionDetalle = 102, Monto = 0m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 6, IdCotizacion = 1, IdProveedor = 9, IdCotizacionDetalle = 102, Monto = -10m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 7, IdCotizacion = 3, IdProveedor = 7, IdRenglon = 301, Monto = 120m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 8, IdCotizacion = 3, IdProveedor = 8, IdRenglon = 301, Monto = 150m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 9, IdCotizacion = 3, IdProveedor = 9, IdRenglon = 301, Monto = 180m, FechaOferta = DateTime.Now, FecBaja = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 10, IdCotizacion = 3, IdProveedor = 9, IdRenglon = 302, Monto = 40m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 11, IdCotizacion = 3, IdProveedor = 9, IdRenglon = 302, Monto = 0m, FechaOferta = DateTime.Now },
            new TOfertaSubasta { IdOfertaSubasta = 12, IdCotizacion = 3, IdProveedor = 9, IdRenglon = 302, Monto = -10m, FechaOferta = DateTime.Now });
        await context.SaveChangesAsync();
        var service = CreateService(context, organizationId: 10);

        var ownResult = await service.GetResumenOfertasAsync(1);
        var directResult = await service.GetResumenOfertasAsync(3);
        var foreignResult = await service.GetResumenOfertasAsync(2);
        var deletedResult = await service.GetResumenOfertasAsync(4);

        ownResult.Success.Should().BeTrue();
        ownResult.Data!.CantidadOfertas.Should().Be(3);
        ownResult.Data.MejorOferta.Should().Be(145m);
        ownResult.Data.Estado.Should().Be("Enviada Pendiente");
        typeof(SubastaResumenOfertasDto).GetProperties().Select(p => p.Name)
            .Should().NotContain(["Ofertas", "IdOfertaSubasta", "IdProveedor", "Proveedor"]);
        directResult.Data!.CantidadOfertas.Should().Be(3);
        directResult.Data.MejorOferta.Should().Be(190m);
        foreignResult.Code.Should().Be(404);
        deletedResult.Code.Should().Be(404);
    }

    [Theory]
    [InlineData("search")]
    [InlineData("in-progress")]
    [InlineData("upcoming")]
    [InlineData("month")]
    public async Task ListRead_OrganizationUser_ReturnsOnlyOwnOrganization(string operation)
    {
        await using var context = CreateContext();
        SeedOrganizationAuctions(context, operation);
        await context.SaveChangesAsync();
        var service = CreateService(context, organizationId: 10);

        var ids = operation switch
        {
            "search" => (await service.BuscarAsync(null, null, null, null, null, null, null)).Data!.Select(x => x.IdCotizacion),
            "in-progress" => (await service.GetSubastasEnCursoAsync(null)).Data!.Select(x => x.IdCotizacion),
            "upcoming" => (await service.GetSubastasProximasAsync(null)).Data!.Select(x => x.IdCotizacion),
            "month" => (await service.GetSubastasDelMesAsync(null)).Data!.Select(x => x.IdCotizacion),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        ids.Should().Equal(1);
    }

    [Fact]
    public async Task GetByIdAsync_CrossOrganization_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.TCotizaciones.AddRange(CreateAuction(1, 10), CreateAuction(2, 20));
        await context.SaveChangesAsync();
        var service = CreateService(context, organizationId: 10);

        var ownResult = await service.GetByIdAsync(1);
        var foreignResult = await service.GetByIdAsync(2);

        ownResult.Success.Should().BeTrue();
        ownResult.Data!.IdCotizacion.Should().Be(1);
        foreignResult.Success.Should().BeFalse();
        foreignResult.Code.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_AuthorizedProvider_DoesNotExposeOtherProviders()
    {
        await using var context = CreateContext();
        var auction = CreateAuction(1, 10, visibility: "0", invitedProviderId: 7);
        auction.Proveedores.Add(new TCotizacionProveedor
        {
            IdCotizacionProveedor = 2,
            IdCotizacion = 1,
            IdProveedor = 8,
            Ganadora = "N"
        });
        context.TCotizaciones.Add(auction);
        await context.SaveChangesAsync();

        var result = await CreateService(context, providerId: 7).GetByIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Proveedores.Should().ContainSingle(p => p.IdProveedor == 7);
        result.Data.Proveedores.Should().NotContain(p => p.IdProveedor == 8);
    }

    [Fact]
    public async Task GetByIdAsync_OrganizationUser_ReturnsAllProvidersForOwnAuction()
    {
        await using var context = CreateContext();
        var auction = CreateAuction(1, 10, visibility: "0", invitedProviderId: 7);
        auction.Proveedores.Add(new TCotizacionProveedor
        {
            IdCotizacionProveedor = 2,
            IdCotizacion = 1,
            IdProveedor = 8,
            Ganadora = "N"
        });
        context.TCotizaciones.Add(auction);
        await context.SaveChangesAsync();

        var result = await CreateService(context, organizationId: 10).GetByIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Proveedores.Select(p => p.IdProveedor).Should().BeEquivalentTo([7, 8]);
    }

    [Fact]
    public async Task BuscarAsync_Provider_ReturnsOnlyPublicAndInvitedAuctions()
    {
        await using var context = CreateContext();
        context.TCotizaciones.AddRange(
            CreateAuction(1, 10, visibility: "1"),
            CreateAuction(2, 20, visibility: "0", invitedProviderId: 7),
            CreateAuction(3, 30, visibility: "0"));
        await context.SaveChangesAsync();

        var result = await CreateService(context, providerId: 7)
            .BuscarAsync(null, null, null, null, null, null, null);

        result.Success.Should().BeTrue();
        result.Data!.Select(x => x.IdCotizacion).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task BuscarAsync_SuperAdmin_ReturnsAllOrganizations()
    {
        await using var context = CreateContext();
        context.TCotizaciones.AddRange(CreateAuction(1, 10), CreateAuction(2, 20));
        await context.SaveChangesAsync();

        var result = await CreateService(context, isSuperAdmin: true)
            .BuscarAsync(null, null, null, null, null, null, null);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("search")]
    [InlineData("in-progress")]
    [InlineData("upcoming")]
    [InlineData("month")]
    [InlineData("detail")]
    public async Task ReadOperation_UserWithoutVisibilityClaim_FailsClosed(string operation)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var code = operation switch
        {
            "search" => (await service.BuscarAsync(null, null, null, null, null, null, null)).Code,
            "in-progress" => (await service.GetSubastasEnCursoAsync(null)).Code,
            "upcoming" => (await service.GetSubastasProximasAsync(null)).Code,
            "month" => (await service.GetSubastasDelMesAsync(null)).Code,
            "detail" => (await service.GetByIdAsync(1)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(401);
    }

    private static PortalSubastasContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase($"CotizacionReadVisibility-{Guid.NewGuid()}")
            .Options;
        return new PortalSubastasContext(options);
    }

    private static CotizacionService CreateService(
        PortalSubastasContext context,
        int? organizationId = null,
        int? providerId = null,
        bool isSuperAdmin = false)
    {
        var claims = new List<Claim>();
        if (organizationId.HasValue)
            claims.Add(new Claim("IdOrganizacion", organizationId.Value.ToString()));
        if (providerId.HasValue)
            claims.Add(new Claim("IdProveedor", providerId.Value.ToString()));
        if (isSuperAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "SUPERADMIN"));

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CotizacionProfile>()).CreateMapper();

        return new CotizacionService(
            context,
            mapper,
            accessor,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IProviderLookupService>(),
            Mock.Of<IPublishEndpoint>(),
            Mock.Of<ILogger<CotizacionService>>(),
            Mock.Of<IProveedorRepresentanteService>());
    }

    private static void SeedOrganizationAuctions(PortalSubastasContext context, string operation)
    {
        var (start, end) = operation switch
        {
            "in-progress" => (DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1)),
            "upcoming" => (DateTime.Now.AddDays(1), DateTime.Now.AddDays(2)),
            "month" => (DateTime.Today, DateTime.Today.AddHours(1)),
            _ => (DateTime.Today, DateTime.Today.AddHours(1))
        };

        context.TCotizaciones.AddRange(
            CreateAuction(1, 10, start: start, end: end),
            CreateAuction(2, 20, start: start, end: end));
    }

    private static TCotizacion CreateAuction(
        int id,
        int organizationId,
        string visibility = "1",
        int? invitedProviderId = null,
        DateTime? start = null,
        DateTime? end = null,
        int tipoContratacion = (int)TipoContratacion.SubastaInversa,
        int? criterioAdjudicacion = null,
        bool deleted = false)
    {
        var auction = new TCotizacion
        {
            IdCotizacion = id,
            NroCotizacion = $"2026/{id:000000}",
            IdEstado = 39,
            IdTipoContratacion = tipoContratacion,
            IdVigencia = 1,
            IdOrganizacion = organizationId,
            IdUnidadAdm = id,
            IdUnidadAdmNavigation = new TUnidadesAdministrativa
            {
                IdUnidadAdm = id,
                NumeroUnidadAdm = id,
                NombreUnidadAdm = $"Unit {id}",
                IdVigencia = 1,
                IdOrganizacion = organizationId
            },
            Observacion = $"Auction {id}",
            FecBaja = deleted ? DateTime.Now : null,
            Especificacion = new TCotizacionEspecificacion
            {
                IdCotEspecificacion = id,
                IdCotizacion = id,
                NroExpediente = $"EXP-{id}",
                Redeterminacion = visibility,
                CriterioAdjudicacion = criterioAdjudicacion,
                FechaInicioSubasta = start ?? DateTime.Today,
                FechaFinalizacionSubasta = end ?? DateTime.Today.AddHours(1)
            }
        };

        if (invitedProviderId.HasValue)
        {
            auction.Proveedores.Add(new TCotizacionProveedor
            {
                IdCotizacionProveedor = id,
                IdCotizacion = id,
                IdProveedor = invitedProviderId.Value,
                Ganadora = "N"
            });
        }

        return auction;
    }
}
