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
using Moq;
using PortalSubastas.Contracts.Events;
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
    public async Task LifecycleMutation_WithoutSuperAdmin_FailsClosedWithoutChangingData(string operation, string? role)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotificarAsync_WithoutActiveAssignedProviders_ReturnsBadRequestAndKeepsGenerated(bool softDeletedRelation)
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction());
        if (softDeletedRelation)
        {
            context.TCotizacionProveedores.Add(new TCotizacionProveedor
            {
                IdCotizacionProveedor = 1,
                IdCotizacion = 1,
                IdProveedor = 99,
                Ganadora = "N",
                FecBaja = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var result = await CreateService(context, "SUPERADMIN").NotificarAsync(1);

        result.Code.Should().Be(400);
        (await context.TCotizaciones.SingleAsync()).IdEstado.Should().Be(4);
    }

    [Fact]
    public async Task NotificarAsync_WithoutRepresentativeRecipients_ReturnsBadRequestAndKeepsGenerated()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction());
        context.TCotizacionProveedores.Add(new TCotizacionProveedor
        {
            IdCotizacionProveedor = 1,
            IdCotizacion = 1,
            IdProveedor = 99,
            Ganadora = "N"
        });
        await context.SaveChangesAsync();

        var result = await CreateService(context, "SUPERADMIN").NotificarAsync(1);

        result.Code.Should().Be(400);
        (await context.TCotizaciones.SingleAsync()).IdEstado.Should().Be(4);
    }

    [Fact]
    public async Task NotificarAsync_WithRecipients_PublishesOnceAndTransitionsGeneratedAuction()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction());
        context.TCotizacionProveedores.Add(new TCotizacionProveedor
        {
            IdCotizacionProveedor = 1,
            IdCotizacion = 1,
            IdProveedor = 99,
            Ganadora = "N"
        });
        await context.SaveChangesAsync();
        var publisher = new Mock<IPublishEndpoint>();
        var representatives = new Mock<IProveedorRepresentanteService>();
        representatives.Setup(service => service.GetRepresentantesAsync(99))
            .ReturnsAsync([("recipient@example.com", "Recipient")]);
        var service = CreateService(context, "SUPERADMIN", publisher, representatives);

        var result = await service.NotificarAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.IdEstado.Should().Be(39);
        (await context.TCotizaciones.SingleAsync()).IdEstado.Should().Be(39);
        publisher.Verify(endpoint => endpoint.Publish(
                It.Is<SubastaPublicadaEvent>(@event => @event.IdCotizacion == 1
                    && @event.Proveedores.Single().IdProveedor == 99
                    && @event.Proveedores.Single().EmailProveedor == "recipient@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    public async Task NotificarAsync_SerializesConcurrentProviderMutations(string operation)
    {
        var databaseName = $"PublicationConcurrency-{Guid.NewGuid()}";
        await using (var seed = CreateContext(databaseName))
        {
            seed.TCotizaciones.Add(CreateAuction());
            seed.TCotizacionProveedores.Add(new TCotizacionProveedor
            {
                IdCotizacionProveedor = 1, IdCotizacion = 1, IdProveedor = 99, Ganadora = "N"
            });
            await seed.SaveChangesAsync();
        }

        var lookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var representatives = new Mock<IProveedorRepresentanteService>();
        representatives.Setup(service => service.GetRepresentantesAsync(99)).Returns(async () =>
        {
            lookupStarted.SetResult();
            await releaseLookup.Task;
            return [("recipient@example.com", "Recipient")];
        });

        await using var publishContext = CreateContext(databaseName);
        await using var mutationContext = CreateContext(databaseName);
        var publishTask = CreateService(publishContext, "SUPERADMIN", representatives: representatives).NotificarAsync(1);
        await lookupStarted.Task;

        var mutationService = CreateProviderService(mutationContext, "SUPERADMIN");
        var mutationTask = operation == "add"
            ? AddAndGetCodeAsync(mutationService)
            : RemoveAndGetCodeAsync(mutationService);
        await Task.Yield();
        mutationTask.IsCompleted.Should().BeFalse();

        releaseLookup.SetResult();
        (await publishTask).Code.Should().Be(200);
        (await mutationTask).Should().Be(400);
        (await mutationContext.TCotizacionProveedores.SingleAsync()).FecBaja.Should().BeNull();
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

    private static PortalSubastasContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName ?? $"CotizacionLifecycle-{Guid.NewGuid()}")
            .Options;
        return new PortalSubastasContext(options);
    }

    private static ProveedorService CreateProviderService(PortalSubastasContext context, string role)
    {
        var representatives = new Mock<IProveedorRepresentanteService>();
        representatives.Setup(service => service.GetRepresentantesAsync(It.IsAny<int>()))
            .ReturnsAsync([("provider@example.com", "Provider")]);
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, "phase3-test"), new(ClaimTypes.Role, role)], "Test"))
            }
        };
        return new ProveedorService(context, Mock.Of<IMapper>(), accessor, new MemoryCache(new MemoryCacheOptions()), representatives.Object);
    }

    private static async Task<int?> AddAndGetCodeAsync(ProveedorService service)
        => (await service.AddProveedorAsync(1, new() { IdProveedor = 98 })).Code;

    private static async Task<int?> RemoveAndGetCodeAsync(ProveedorService service)
        => (await service.RemoveProveedorAsync(1, 1)).Code;

    private static CotizacionService CreateService(
        PortalSubastasContext context,
        string? role,
        Mock<IPublishEndpoint>? publisher = null,
        Mock<IProveedorRepresentanteService>? representatives = null)
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
            publisher?.Object ?? Mock.Of<IPublishEndpoint>(),
            Mock.Of<ILogger<CotizacionService>>(),
            CreateRepresentatives(representatives));
    }

    private static IProveedorRepresentanteService CreateRepresentatives(Mock<IProveedorRepresentanteService>? representatives)
    {
        if (representatives is not null)
            return representatives.Object;

        var emptyRepresentatives = new Mock<IProveedorRepresentanteService>();
        emptyRepresentatives.Setup(service => service.GetRepresentantesAsync(It.IsAny<int>()))
            .ReturnsAsync([]);
        return emptyRepresentatives.Object;
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
