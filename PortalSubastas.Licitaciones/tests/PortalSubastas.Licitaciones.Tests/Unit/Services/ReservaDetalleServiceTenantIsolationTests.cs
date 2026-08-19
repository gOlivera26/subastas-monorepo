using System.Security.Claims;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalSubastas.Licitaciones.Application.AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class ReservaDetalleServiceTenantIsolationTests
{
    [Fact]
    public async Task GetByReservaIdAsync_CrossOrganizationReserva_ReturnsNotFound()
    {
        await using var context = CreateContextWithForeignDetail();

        var result = await CreateService(context, organizationId: 10).GetByReservaIdAsync(2);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_CrossOrganizationDetail_ReturnsNotFound()
    {
        await using var context = CreateContextWithForeignDetail();

        var result = await CreateService(context, organizationId: 10).GetByIdAsync(50);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_RegularUserWithoutOrganizationClaim_FailsClosed()
    {
        await using var context = CreateContextWithForeignDetail();

        var result = await CreateService(context).GetByIdAsync(50);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(401);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("deauthorize")]
    public async Task Operation_RegularUserWithoutOrganizationClaim_FailsClosed(string operation)
    {
        await using var context = CreateContextWithForeignDetail();
        var service = CreateService(context);
        var request = new ReservaDetalleRequestDto
        {
            IdReserva = 2,
            IdItem = 1
        };

        var code = operation switch
        {
            "list" => (await service.GetByReservaIdAsync(2)).Code,
            "create" => (await service.CreateAsync(request)).Code,
            "update" => (await service.UpdateAsync(50, request)).Code,
            "delete" => (await service.DeleteAsync(50)).Code,
            "deauthorize" => (await service.DesautorizarAsync(50)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(401);
    }

    [Fact]
    public async Task GetByIdAsync_SuperAdmin_CanReadAnyOrganization()
    {
        await using var context = CreateContextWithForeignDetail();

        var result = await CreateService(context, isSuperAdmin: true).GetByIdAsync(50);

        result.Success.Should().BeTrue();
        result.Data!.IdReservaDet.Should().Be(50);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("deauthorize")]
    public async Task Mutation_CrossOrganizationParent_ReturnsNotFoundWithoutChangingData(string operation)
    {
        await using var context = CreateContextWithForeignDetail();
        var service = CreateService(context, organizationId: 10);
        var request = new ReservaDetalleRequestDto
        {
            IdReserva = 2,
            IdItem = 99,
            Cantidad = 8,
            Importe = 500
        };

        var code = operation switch
        {
            "create" => (await service.CreateAsync(request)).Code,
            "update" => (await service.UpdateAsync(50, request)).Code,
            "delete" => (await service.DeleteAsync(50)).Code,
            "deauthorize" => (await service.DesautorizarAsync(50)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(404);
        var persisted = await context.TReservaDetalles.IgnoreQueryFilters()
            .SingleAsync(d => d.IdReservaDet == 50);
        persisted.IdItem.Should().Be(1);
        persisted.IdEstado.Should().Be(1);
        persisted.FecBaja.Should().BeNull();
        context.TReservaDetalles.IgnoreQueryFilters().Should().ContainSingle();
    }

    [Theory]
    [InlineData("item")]
    [InlineData("category")]
    [InlineData("expenseObject")]
    public async Task CreateAsync_CrossOrganizationReference_ReturnsNotFound(string reference)
    {
        await using var context = CreateContextWithOwnedReservationAndReferences();
        var request = CreateRequestWithGlobalReferences(reservaId: 2);
        SetForeignReference(request, reference);

        var result = await CreateService(context, organizationId: 10).CreateAsync(request);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
        context.TReservaDetalles.IgnoreQueryFilters().Should().ContainSingle();
    }

    [Theory]
    [InlineData("item")]
    [InlineData("category")]
    [InlineData("expenseObject")]
    public async Task UpdateAsync_CrossOrganizationReference_ReturnsNotFoundWithoutChangingData(string reference)
    {
        await using var context = CreateContextWithOwnedReservationAndReferences();
        var request = CreateRequestWithGlobalReferences(reservaId: 2);
        SetForeignReference(request, reference);

        var result = await CreateService(context, organizationId: 10).UpdateAsync(50, request);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
        var persisted = await context.TReservaDetalles.IgnoreQueryFilters()
            .SingleAsync(d => d.IdReservaDet == 50);
        persisted.IdItem.Should().Be(10);
        persisted.IdCatProg.Should().Be(11);
        persisted.IdObjetoGasto.Should().Be(12);
    }

    private static PortalSubastasContext CreateContextWithForeignDetail()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase($"ReservaDetalleTenantIsolation-{Guid.NewGuid()}")
            .Options;
        var context = new PortalSubastasContext(options);
        context.TVigencias.Add(new TVigencia
        {
            IdVigencia = 1,
            Ejercicio = 2026,
            ActivoEjecucion = true
        });
        context.TEstados.Add(new TEstado
        {
            IdEstado = 1,
            Descripcion = "Generated",
            Tipo = "RESERVA"
        });
        context.TUnidadesAdministrativas.Add(new TUnidadesAdministrativa
        {
            IdUnidadAdm = 2,
            NumeroUnidadAdm = 2,
            NombreUnidadAdm = "Foreign unit",
            IdVigencia = 1,
            IdOrganizacion = 20
        });
        context.TCatalogosBiens.Add(new TCatalogosBien
        {
            IdItem = 1,
            Codigo = "ITEM-1",
            NItem = "Item 1",
            IdVigencia = 1,
            IdOrganizacion = 20
        });
        context.TReservas.Add(new TReserva
        {
            IdReserva = 2,
            NroReserva = "2026/000002",
            IdVigencia = 1,
            IdUnidadAdm = 2,
            IdOrganizacion = 20,
            IdEstado = 1,
            FechaReserva = new DateOnly(2026, 1, 1)
        });
        context.TReservaDetalles.Add(new TReservaDetalle
        {
            IdReservaDet = 50,
            IdReserva = 2,
            IdItem = 1,
            IdEstado = 1
        });
        context.SaveChanges();
        return context;
    }

    private static PortalSubastasContext CreateContextWithOwnedReservationAndReferences()
    {
        var context = CreateContextWithForeignDetail();
        context.TReservas.Single().IdOrganizacion = 10;
        context.TUnidadesAdministrativas.Single().IdOrganizacion = 10;
        var detail = context.TReservaDetalles.Single();
        detail.IdItem = 10;
        detail.IdCatProg = 11;
        detail.IdObjetoGasto = 12;
        context.TCatalogosBiens.Add(new TCatalogosBien
        {
            IdItem = 10,
            Codigo = "GLOBAL-ITEM",
            NItem = "Global item",
            IdVigencia = 1,
            IdOrganizacion = null
        });
        context.TCategoriasProgramaticas.AddRange(
            new TCategoriasProgramatica
            {
                IdCatProg = 11,
                IdVigencia = 1,
                Codigo = 11,
                Nombre = "Global category",
                Naturaleza = "Global",
                IdOrganizacion = null
            },
            new TCategoriasProgramatica
            {
                IdCatProg = 21,
                IdVigencia = 1,
                Codigo = 21,
                Nombre = "Foreign category",
                Naturaleza = "Foreign",
                IdOrganizacion = 20
            });
        context.TObjetosGastos.AddRange(
            new TObjetosGasto
            {
                IdObjetoGasto = 12,
                NumeroObjeto = "GLOBAL-EXPENSE",
                NombreObjeto = "Global expense object",
                IdVigencia = 1,
                IdOrganizacion = null
            },
            new TObjetosGasto
            {
                IdObjetoGasto = 22,
                NumeroObjeto = "FOREIGN-EXPENSE",
                NombreObjeto = "Foreign expense object",
                IdVigencia = 1,
                IdOrganizacion = 20
            });
        context.SaveChanges();
        return context;
    }

    private static ReservaDetalleRequestDto CreateRequestWithGlobalReferences(int reservaId) => new()
    {
        IdReserva = reservaId,
        IdItem = 10,
        IdCatProg = 11,
        IdObjetoGasto = 12,
        Cantidad = 1,
        Importe = 100
    };

    private static void SetForeignReference(ReservaDetalleRequestDto request, string reference)
    {
        switch (reference)
        {
            case "item":
                request.IdItem = 1;
                break;
            case "category":
                request.IdCatProg = 21;
                break;
            case "expenseObject":
                request.IdObjetoGasto = 22;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reference));
        }
    }

    private static ReservaDetalleService CreateService(
        PortalSubastasContext context,
        int? organizationId = null,
        bool isSuperAdmin = false)
    {
        var claims = new List<Claim>();
        if (organizationId.HasValue)
            claims.Add(new Claim("IdOrganizacion", organizationId.Value.ToString()));
        if (isSuperAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "SUPERADMIN"));

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ReservaProfile>()).CreateMapper();

        return new ReservaDetalleService(
            context,
            mapper,
            accessor,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IPublishEndpoint>());
    }
}
