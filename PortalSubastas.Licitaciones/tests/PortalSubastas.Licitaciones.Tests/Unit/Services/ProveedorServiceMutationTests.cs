using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PortalSubastas.Licitaciones.API.Controllers;
using PortalSubastas.Licitaciones.Application.RequestDto.Proveedor;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class ProveedorServiceMutationTests
{
    [Theory]
    [InlineData(null, 4, 401)]
    [InlineData("SUPERADMIN", 39, 400)]
    public async Task AddProveedorAsync_RejectsUnauthorizedOrNonGeneratedAuction(string? role, int auctionState, int expectedCode)
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction(auctionState));
        await context.SaveChangesAsync();

        var result = await CreateService(context, role).AddProveedorAsync(1, new ProveedorAddDto { IdProveedor = 99 });

        result.Code.Should().Be(expectedCode);
        (await context.TCotizacionProveedores.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddProveedorAsync_SoftDeletedAuction_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction(4, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var result = await CreateService(context, "SUPERADMIN").AddProveedorAsync(1, new ProveedorAddDto { IdProveedor = 99 });

        result.Code.Should().Be(404);
        (await context.TCotizacionProveedores.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(null, 4, 401)]
    [InlineData("SUPERADMIN", 39, 400)]
    public async Task RemoveProveedorAsync_RejectsUnauthorizedOrNonGeneratedAuction(string? role, int auctionState, int expectedCode)
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction(auctionState));
        context.TCotizacionProveedores.Add(new TCotizacionProveedor
        {
            IdCotizacionProveedor = 1,
            IdCotizacion = 1,
            IdProveedor = 99,
            Ganadora = "N"
        });
        await context.SaveChangesAsync();

        var result = await CreateService(context, role).RemoveProveedorAsync(1, 1);

        result.Code.Should().Be(expectedCode);
        (await context.TCotizacionProveedores.IgnoreQueryFilters().SingleAsync()).FecBaja.Should().BeNull();
    }
    [Fact]
    public async Task RemoveProveedorAsync_RejectsForeignOrSoftDeletedRelations()
    {
        await using var context = CreateContext();
        context.TCotizaciones.AddRange(CreateAuction(4), CreateAuction(4, id: 2));
        context.TCotizacionProveedores.AddRange(
            new TCotizacionProveedor { IdCotizacionProveedor = 1, IdCotizacion = 2, IdProveedor = 99, Ganadora = "N" },
            new TCotizacionProveedor { IdCotizacionProveedor = 2, IdCotizacion = 1, IdProveedor = 98, Ganadora = "N", FecBaja = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var service = CreateService(context, "SUPERADMIN");

        var foreign = await service.RemoveProveedorAsync(1, 1);
        var deleted = await service.RemoveProveedorAsync(1, 2);

        foreign.Code.Should().Be(404);
        deleted.Code.Should().Be(404);
        (await context.TCotizacionProveedores.IgnoreQueryFilters().SingleAsync(p => p.IdCotizacionProveedor == 1)).FecBaja.Should().BeNull();
    }

    [Fact]
    public async Task RemoveProveedorAsync_GeneratedAuction_SoftDeletesActiveRelation()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction(4));
        context.TCotizacionProveedores.Add(new TCotizacionProveedor
        {
            IdCotizacionProveedor = 1,
            IdCotizacion = 1,
            IdProveedor = 99,
            Ganadora = "N"
        });
        await context.SaveChangesAsync();

        var result = await CreateService(context, "SUPERADMIN").RemoveProveedorAsync(1, 1);

        result.Success.Should().BeTrue();
        result.Code.Should().Be(200);
        (await context.TCotizacionProveedores.IgnoreQueryFilters().SingleAsync()).FecBaja.Should().NotBeNull();
    }

    [Fact]
    public async Task AddProveedorAsync_AddsProviderThenRejectsDuplicate()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction(4));
        await context.SaveChangesAsync();
        var service = CreateService(context, "SUPERADMIN");

        var added = await service.AddProveedorAsync(1, new ProveedorAddDto { IdProveedor = 99 });
        var duplicate = await service.AddProveedorAsync(1, new ProveedorAddDto { IdProveedor = 99 });

        added.Code.Should().Be(200);
        duplicate.Code.Should().Be(400);
        (await context.TCotizacionProveedores.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AddProveedorAsync_WithoutRepresentatives_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        context.TCotizaciones.Add(CreateAuction(4));
        await context.SaveChangesAsync();
        var representatives = new Mock<IProveedorRepresentanteService>();
        representatives.Setup(service => service.GetRepresentantesAsync(99)).ReturnsAsync([]);
        var service = CreateService(context, "SUPERADMIN", representatives.Object);

        var result = await service.AddProveedorAsync(1, new ProveedorAddDto { IdProveedor = 99 });

        result.Code.Should().Be(400);
        (await context.TCotizacionProveedores.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    public async Task Add_MapsServiceOperationCodeToHttpStatus(int code)
    {
        await using var context = CreateContext();
        var service = new Mock<IProveedorService>();
        var response = code == 200
            ? OperationResponse<object>.SuccessResponse(new object())
            : OperationResponse<object>.CustomErrorResponse(code, "expected");
        service.Setup(candidate => candidate.AddProveedorAsync(1, It.IsAny<ProveedorAddDto>())).ReturnsAsync(response);
        var controller = new ProveedorController(context, service.Object);

        var result = await controller.Add(1, new ProveedorAddDto { IdProveedor = 99 });

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(code);
    }
    private static PortalSubastasContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase($"ProveedorMutation-{Guid.NewGuid()}")
            .Options;
        return new PortalSubastasContext(options);
    }

    private static ProveedorService CreateService(PortalSubastasContext context, string? role, IProveedorRepresentanteService? representativeService = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "phase3-test") };
        if (role is not null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var representatives = new Mock<IProveedorRepresentanteService>();
        representatives.Setup(service => service.GetRepresentantesAsync(It.IsAny<int>()))
            .ReturnsAsync([("provider@example.com", "Provider")]);

        return new ProveedorService(
            context,
            Mock.Of<IMapper>(),
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            },
            new MemoryCache(new MemoryCacheOptions()),
            representativeService ?? representatives.Object);
    }

    private static TCotizacion CreateAuction(int state, DateTime? deletedAt = null, int id = 1) => new()
    {
        IdCotizacion = id,
        NroCotizacion = $"2026/{id:D6}",
        IdEstado = state,
        IdTipoContratacion = (int)TipoContratacion.SubastaInversa,
        IdVigencia = 1,
        IdOrganizacion = 10,
        IdUnidadAdm = 1,
        FecBaja = deletedAt
    };
}
