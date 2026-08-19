using System.Security.Claims;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalSubastas.Licitaciones.Application.AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.Reserva;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class ReservaServiceTenantIsolationTests
{
    [Fact]
    public async Task GetAllAsync_RegularUser_ReturnsOnlyOwnOrganization()
    {
        await using var context = CreateContext();
        SeedReservaReferences(context);
        context.TReservas.AddRange(CreateReserva(1, 10), CreateReserva(2, 20));
        await context.SaveChangesAsync();

        var result = await CreateService(context, organizationId: 10).GetAllAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle(r => r.IdReserva == 1 && r.IdOrganizacion == 10);
    }

    [Fact]
    public async Task GetAllAsync_RegularUserWithoutOrganizationClaim_FailsClosed()
    {
        await using var context = CreateContext();

        var result = await CreateService(context).GetAllAsync();

        result.Success.Should().BeFalse();
        result.Code.Should().Be(401);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("authorize")]
    [InlineData("clone")]
    public async Task Mutation_RegularUserWithoutOrganizationClaim_FailsClosed(string operation)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var code = operation switch
        {
            "create" => (await service.CreateAsync(new ReservaRequestDto
            {
                IdUnidadAdm = 1,
                FechaReserva = new DateOnly(2026, 1, 1)
            })).Code,
            "update" => (await service.UpdateAsync(1, new ReservaRequestDto
            {
                IdUnidadAdm = 1,
                FechaReserva = new DateOnly(2026, 1, 1)
            })).Code,
            "delete" => (await service.DeleteAsync(1)).Code,
            "authorize" => (await service.AutorizarAsync(1, new AutorizarReservaDto
            {
                MotivoAutorizacion = "Test"
            })).Code,
            "clone" => (await service.ClonarAsync(1)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(401);
    }

    [Fact]
    public async Task GetAllAsync_SuperAdmin_ReturnsAllOrganizations()
    {
        await using var context = CreateContext();
        SeedReservaReferences(context);
        context.TReservas.AddRange(CreateReserva(1, 10), CreateReserva(2, 20));
        await context.SaveChangesAsync();

        var result = await CreateService(context, isSuperAdmin: true).GetAllAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_CrossOrganizationId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.TReservas.Add(CreateReserva(2, 20));
        await context.SaveChangesAsync();

        var result = await CreateService(context, organizationId: 10).GetByIdAsync(2);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("authorize")]
    [InlineData("clone")]
    public async Task Mutation_CrossOrganizationId_ReturnsNotFoundWithoutChangingData(string operation)
    {
        await using var context = CreateContext();
        var foreignReserva = CreateReserva(2, 20);
        context.TReservas.Add(foreignReserva);
        context.TReservaDetalles.Add(new TReservaDetalle
        {
            IdReservaDet = 50,
            IdReserva = 2,
            IdItem = 1,
            IdEstado = 1
        });
        await context.SaveChangesAsync();
        var service = CreateService(context, organizationId: 10);

        var code = operation switch
        {
            "update" => (await service.UpdateAsync(2, new ReservaRequestDto
            {
                IdUnidadAdm = 99,
                FechaReserva = new DateOnly(2026, 1, 2)
            })).Code,
            "delete" => (await service.DeleteAsync(2)).Code,
            "authorize" => (await service.AutorizarAsync(2, new AutorizarReservaDto
            {
                MotivoAutorizacion = "Test"
            })).Code,
            "clone" => (await service.ClonarAsync(2)).Code,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        code.Should().Be(404);
        var persisted = await context.TReservas.IgnoreQueryFilters().SingleAsync(r => r.IdReserva == 2);
        persisted.IdUnidadAdm.Should().Be(2);
        persisted.IdEstado.Should().Be(1);
        persisted.FecBaja.Should().BeNull();
        context.TReservas.IgnoreQueryFilters().Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_ForeignOrganizationUnit_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.TVigencias.Add(CreateVigencia());
        context.TUnidadesAdministrativas.Add(CreateUnidad(2, 20));
        await context.SaveChangesAsync();

        var result = await CreateService(context, organizationId: 10).CreateAsync(new ReservaRequestDto
        {
            IdUnidadAdm = 2,
            FechaReserva = new DateOnly(2026, 1, 1)
        });

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
        context.TReservas.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ForeignOrganizationSubResponsable_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.TVigencias.Add(CreateVigencia());
        context.TUnidadesAdministrativas.AddRange(CreateUnidad(1, 10), CreateUnidad(2, 20));
        context.TSubResponsables.Add(new TSubResponsable
        {
            IdSubResponsable = 7,
            IdUnidadAdm = 2,
            Codigo = "SUB-7",
            Nombre = "Foreign"
        });
        await context.SaveChangesAsync();

        var result = await CreateService(context, organizationId: 10).CreateAsync(new ReservaRequestDto
        {
            IdUnidadAdm = 1,
            IdSubResponsable = 7,
            FechaReserva = new DateOnly(2026, 1, 1)
        });

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
        context.TReservas.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ForeignOrganizationReferences_ReturnsNotFoundWithoutChangingData()
    {
        await using var context = CreateContext();
        context.TReservas.Add(CreateReserva(1, 10));
        context.TUnidadesAdministrativas.AddRange(CreateUnidad(1, 10), CreateUnidad(2, 20));
        await context.SaveChangesAsync();

        var result = await CreateService(context, organizationId: 10).UpdateAsync(1, new ReservaRequestDto
        {
            IdUnidadAdm = 2,
            FechaReserva = new DateOnly(2026, 2, 1)
        });

        result.Success.Should().BeFalse();
        result.Code.Should().Be(404);
        (await context.TReservas.SingleAsync()).IdUnidadAdm.Should().Be(1);
    }

    private static PortalSubastasContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase($"ReservaTenantIsolation-{Guid.NewGuid()}")
            .Options;
        return new PortalSubastasContext(options);
    }

    private static ReservaService CreateService(
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

        return new ReservaService(
            context,
            mapper,
            accessor,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IPublishEndpoint>());
    }

    private static TReserva CreateReserva(int id, int organizationId) => new()
    {
        IdReserva = id,
        NroReserva = $"2026/{id:000000}",
        IdVigencia = 1,
        IdUnidadAdm = id,
        IdOrganizacion = organizationId,
        IdEstado = 1,
        FechaReserva = new DateOnly(2026, 1, 1)
    };

    private static TUnidadesAdministrativa CreateUnidad(int id, int organizationId) => new()
    {
        IdUnidadAdm = id,
        NumeroUnidadAdm = id,
        NombreUnidadAdm = $"Unit {id}",
        IdVigencia = 1,
        IdOrganizacion = organizationId
    };

    private static TVigencia CreateVigencia() => new()
    {
        IdVigencia = 1,
        Ejercicio = 2026,
        ActivoEjecucion = true
    };

    private static void SeedReservaReferences(PortalSubastasContext context)
    {
        context.TVigencias.Add(CreateVigencia());
        context.TEstados.Add(new TEstado
        {
            IdEstado = 1,
            Descripcion = "Generated",
            Tipo = "RESERVA"
        });
        context.TUnidadesAdministrativas.AddRange(CreateUnidad(1, 10), CreateUnidad(2, 20));
    }
}
