using System.Security.Claims;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Licitaciones.Application.RequestDto.OfertaSubasta;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class OfertaSubastaServiceConcurrencyTests
{
    [Fact]
    public async Task ProcesarOfertasAsync_DuplicateOffer_IsDedupedAndReturnsError()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuctionInCourse());
        context.TReservaDetalles.Add(CreateReservaDetalle());
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var offer = new OfertaItemRequestDto { IdCotizacionDetalle = 1, Monto = 900m, IdMonedaOferta = 1 };

        var first = await service.ProcesarOfertasAsync(1, new() { offer });
        var second = await service.ProcesarOfertasAsync(1, new() { offer });

        first.Code.Should().Be(200);
        first.Data.Should().ContainSingle().Which.TextoError.Should().BeNull();

        second.Code.Should().Be(200);
        second.Data.Should().ContainSingle();
        second.Data![0].TextoError.Should().Be("Ya registraste una oferta idéntica para este ítem.");

        (await context.TOfertasSubastas.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProcesarOfertasAsync_SequentialDescendingOffers_BothAcceptedThenDuplicateRejected()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuctionInCourse());
        context.TReservaDetalles.Add(CreateReservaDetalle());
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var first = await service.ProcesarOfertasAsync(1, new()
        {
            new OfertaItemRequestDto { IdCotizacionDetalle = 1, Monto = 900m, IdMonedaOferta = 1 }
        });
        var second = await service.ProcesarOfertasAsync(1, new()
        {
            new OfertaItemRequestDto { IdCotizacionDetalle = 1, Monto = 850m, IdMonedaOferta = 1 }
        });
        var duplicate = await service.ProcesarOfertasAsync(1, new()
        {
            new OfertaItemRequestDto { IdCotizacionDetalle = 1, Monto = 850m, IdMonedaOferta = 1 }
        });

        first.Code.Should().Be(200);
        first.Data.Should().ContainSingle().Which.TextoError.Should().BeNull();
        second.Code.Should().Be(200);
        second.Data.Should().ContainSingle().Which.TextoError.Should().BeNull();

        duplicate.Code.Should().Be(200);
        duplicate.Data.Should().ContainSingle();
        duplicate.Data![0].TextoError.Should().Be("Ya registraste una oferta idéntica para este ítem.");

        (await context.TOfertasSubastas.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ProcesarOfertasAsync_TwoProviders_EachOfferAccepted()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuctionInCourse());
        context.TReservaDetalles.Add(CreateReservaDetalle());
        await context.SaveChangesAsync();
        var providerA = CreateService(context, idProveedor: 99);
        var providerB = CreateService(context, idProveedor: 100);

        var offerA = await providerA.ProcesarOfertasAsync(1, new()
        {
            new OfertaItemRequestDto { IdCotizacionDetalle = 1, Monto = 900m, IdMonedaOferta = 1 }
        });
        var offerB = await providerB.ProcesarOfertasAsync(1, new()
        {
            new OfertaItemRequestDto { IdCotizacionDetalle = 1, Monto = 850m, IdMonedaOferta = 1 }
        });

        offerA.Code.Should().Be(200);
        offerB.Code.Should().Be(200);

        var rows = await context.TOfertasSubastas.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(r => r.IdProveedor).OrderBy(id => id).Should().Equal(99, 100);
    }

    private static PortalSubastasContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase($"OfertaSubasta-{Guid.NewGuid()}")
            .Options;
        return new PortalSubastasContext(options);
    }

    private static OfertaSubastaService CreateService(PortalSubastasContext context, int idProveedor = 99)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "oferta-test"),
            new("IdProveedor", idProveedor.ToString())
        };

        var accessor = new Mock<IHttpContextAccessor>();
        accessor
            .Setup(a => a.HttpContext)
            .Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            });

        var notificationService = new Mock<ISubastaNotificationService>();
        notificationService
            .Setup(s => s.NotificarNuevaOfertaAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        notificationService
            .Setup(s => s.NotificarMejorOfertaActualizadaAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);
        notificationService
            .Setup(s => s.NotificarProrrogaAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        notificationService
            .Setup(s => s.NotificarCierrePorTopeAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SystemLogEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var providerLookup = new Mock<IProviderLookupService>();
        providerLookup
            .Setup(s => s.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, ProviderReportLookupDto>());

        return new OfertaSubastaService(
            context,
            Mock.Of<IMapper>(),
            accessor.Object,
            new MemoryCache(new MemoryCacheOptions()),
            notificationService.Object,
            publishEndpoint.Object,
            providerLookup.Object);
    }

    private static TCotizacion CreateAuctionInCourse() => new()
    {
        IdCotizacion = 1,
        NroCotizacion = "2026/000001",
        IdEstado = 39,
        IdTipoContratacion = (int)TipoContratacion.SubastaInversa,
        IdVigencia = 1,
        IdOrganizacion = 10,
        IdUnidadAdm = 1,
        Observacion = "Oferta concurrency fixture",
        Especificacion = new TCotizacionEspecificacion
        {
            IdCotEspecificacion = 1,
            IdCotizacion = 1,
            FechaInicioSubasta = DateTime.Today.AddDays(-1),
            FechaFinalizacionSubasta = DateTime.Today.AddDays(1),
            MargenMejora = 5,
            PermiteProrroga = false
        },
        Detalles = new List<TCotizacionDetalle>
        {
            new()
            {
                IdCotizacionDetalle = 1,
                IdCotizacion = 1,
                IdReservaDetalle = 100,
                IdItem = 1,
                Cantidad = 1,
                ImporteBase = 1000
            }
        }
    };

    private static TReservaDetalle CreateReservaDetalle() => new()
    {
        IdReservaDet = 100,
        IdReserva = 1,
        IdItem = 1,
        IdMoneda = 1
    };
}
