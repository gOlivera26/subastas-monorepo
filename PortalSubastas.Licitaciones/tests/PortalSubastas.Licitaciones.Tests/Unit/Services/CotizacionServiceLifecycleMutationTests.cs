using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PortalSubastas.Licitaciones.API.Controllers;
using PortalSubastas.Licitaciones.Application.AutoMapper;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class CotizacionServiceLifecycleMutationTests
{
    [Theory]
    [InlineData(nameof(CotizacionController.Notificar))]
    [InlineData(nameof(CotizacionController.Delete))]
    public void LifecycleEndpoint_RequiresSuperAdmin(string methodName)
    {
        var method = typeof(CotizacionController).GetMethod(methodName);

        method.Should().NotBeNull();
        method!.GetCustomAttributes<AuthorizeAttribute>()
            .Should().ContainSingle(attribute => attribute.Roles == "SUPERADMIN");
    }

    [Theory]
    [InlineData("publish", null)]
    [InlineData("publish", "ADMIN")]
    [InlineData("cancel", null)]
    [InlineData("cancel", "ADMIN")]
    public async Task LifecycleMutation_WithoutSuperAdmin_FailsClosedWithoutChangingData(
        string operation,
        string? role)
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction());
        await context.SaveChangesAsync();
        var service = CreateService(context, role);

        var code = operation switch
        {
            "publish" => (await service.NotificarAsync(1)).Code,
            "cancel" => (await service.DeleteAsync(1)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(401);
        var persisted = await context.TCotizaciones.IgnoreQueryFilters().SingleAsync();
        persisted.IdEstado.Should().Be(4);
        persisted.FecBaja.Should().BeNull();
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("cancel")]
    public async Task LifecycleMutation_SoftDeletedAuction_ReturnsNotFoundWithoutChangingData(string operation)
    {
        await using var context = CreateContext();
        var deletedAt = new DateTime(2026, 8, 18, 10, 0, 0);
        context.TCotizaciones.Add(CreateAuction(deletedAt));
        await context.SaveChangesAsync();
        var service = CreateService(context, "SUPERADMIN");

        var code = operation switch
        {
            "publish" => (await service.NotificarAsync(1)).Code,
            "cancel" => (await service.DeleteAsync(1)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(404);
        var persisted = await context.TCotizaciones.IgnoreQueryFilters().SingleAsync();
        persisted.IdEstado.Should().Be(4);
        persisted.FecBaja.Should().Be(deletedAt);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("cancel")]
    public async Task LifecycleMutation_InvalidState_ReturnsBadRequestWithoutChangingData(string operation)
    {
        await using var context = CreateContext();
        var auction = CreateAuction();
        auction.IdEstado = 39;
        context.TCotizaciones.Add(auction);
        await context.SaveChangesAsync();
        var service = CreateService(context, "SUPERADMIN");

        var code = operation switch
        {
            "publish" => (await service.NotificarAsync(1)).Code,
            "cancel" => (await service.DeleteAsync(1)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(400);
        (await context.TCotizaciones.SingleAsync()).IdEstado.Should().Be(39);
    }

    [Fact]
    public async Task NotificarAsync_SuperAdmin_TransitionsGeneratedAuctionAndReturnsSafeContract()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction());
        await context.SaveChangesAsync();
        var service = CreateService(context, "SUPERADMIN");

        var result = await service.NotificarAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.NroCotizacion.Should().Be("2026/000001");
        result.Data.IdEstado.Should().Be(39);
        result.Data.Estado.Should().Be("Enviada Pendiente");
        JsonSerializer.SerializeToElement(result.Data).EnumerateObject().Select(property => property.Name)
            .Should().NotContain(name => string.Equals(name, "Proveedores", StringComparison.OrdinalIgnoreCase));
        (await context.TCotizaciones.SingleAsync()).IdEstado.Should().Be(39);
    }

    [Fact]
    public async Task DeleteAsync_SuperAdmin_AnulatesGeneratedAuction()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction());
        await context.SaveChangesAsync();
        var service = CreateService(context, "SUPERADMIN");

        var result = await service.DeleteAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        var persisted = await context.TCotizaciones.IgnoreQueryFilters().SingleAsync();
        persisted.IdEstado.Should().Be(20);
        persisted.FecBaja.Should().NotBeNull();
    }

    private static PortalSubastasContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase($"CotizacionLifecycle-{Guid.NewGuid()}")
            .Options;
        return new PortalSubastasContext(options);
    }

    private static CotizacionService CreateService(PortalSubastasContext context, string? role)
    {
        var claims = new List<Claim>
        {
            new("IdOrganizacion", "10"),
            new(ClaimTypes.Name, "phase3-test")
        };
        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<CotizacionProfile>())
            .CreateMapper();

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

    private static TCotizacion CreateAuction(DateTime? deletedAt = null) => new()
    {
        IdCotizacion = 1,
        NroCotizacion = "2026/000001",
        IdEstado = 4,
        IdTipoContratacion = (int)TipoContratacion.SubastaInversa,
        IdVigencia = 1,
        IdOrganizacion = 10,
        IdUnidadAdm = 1,
        Observacion = "Phase 3 fixture",
        FecBaja = deletedAt,
        Especificacion = new TCotizacionEspecificacion
        {
            IdCotEspecificacion = 1,
            IdCotizacion = 1,
            NroExpediente = "EXP-PHASE3",
            FechaInicioSubasta = DateTime.Today.AddDays(1),
            FechaFinalizacionSubasta = DateTime.Today.AddDays(2)
        }
    };
}
