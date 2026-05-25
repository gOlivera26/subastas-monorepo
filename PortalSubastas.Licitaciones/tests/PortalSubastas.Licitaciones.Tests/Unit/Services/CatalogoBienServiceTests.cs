using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PortalSubastas.Licitaciones.Application.AutoMapper;
using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.Services.Implementations;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Tests.Unit.Services;

public class CatalogoBienServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextMock;
    private readonly MemoryCache _realCache;
    private readonly IMapper _mapper;

    public CatalogoBienServiceTests()
    {
        _httpContextMock = new Mock<IHttpContextAccessor>();
        _realCache = new MemoryCache(new MemoryCacheOptions());

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CatalogosProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Map_EntityToDto_ShouldMapCorrectly()
    {
        var entity = new TCatalogosBien
        {
            IdItem = 1,
            Codigo = "BIEN001",
            NItem = "Test Bien",
            IdOrganizacion = 5,
            IdObjetoGasto = 10,
            FecBaja = null
        };

        var dto = _mapper.Map<CatalogoBienResponseDto>(entity);

        dto.IdItem.Should().Be(1);
        dto.Codigo.Should().Be("BIEN001");
        dto.NItem.Should().Be("Test Bien");
        dto.NumeroJurisdiccion.Should().Be(5);
        dto.IdCategoriaBien.Should().Be(10);
        dto.Vigente.Should().BeTrue();
    }

    [Fact]
    public void Map_EntityWithBaja_ShouldReturnVigenteFalse()
    {
        var entity = new TCatalogosBien
        {
            IdItem = 2,
            Codigo = "BIEN002",
            NItem = "Bien de Baja",
            FecBaja = DateTime.Now.AddDays(-1)
        };

        var dto = _mapper.Map<CatalogoBienResponseDto>(entity);

        dto.Vigente.Should().BeFalse();
    }

    [Fact]
    public async Task GetByFilterAsync_VigenteTrue_ShouldExcludeBajaRecords()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName: "Test_CatalogoBien_VigenteTrue")
            .Options;

        await using var context = new PortalSubastasContext(options);

        context.TCatalogosBiens.AddRange(new List<TCatalogosBien>
        {
            new() { IdItem = 1, Codigo = "BIEN001", NItem = "Bien Vigente 1", IdVigencia = 1, FecBaja = null },
            new() { IdItem = 2, Codigo = "BIEN002", NItem = "Bien Vigente 2", IdVigencia = 1, FecBaja = null },
            new() { IdItem = 3, Codigo = "BIEN003", NItem = "Bien de Baja", IdVigencia = 1, FecBaja = DateTime.Now }
        });
        await context.SaveChangesAsync();

        var service = new CatalogoBienService(context, _mapper, _httpContextMock.Object, _realCache);
        var filtros = new CatalogoBienFilterDto { Vigente = true };

        var result = await service.GetByFilterAsync(filtros);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.All(b => b.Vigente).Should().BeTrue();
        result.Data!.Any(b => b.IdItem == 3).Should().BeFalse();
    }

    [Fact]
    public async Task GetByFilterAsync_NoFilters_ShouldReturnAllOrderedByName()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName: "Test_CatalogoBien_NoFilters")
            .Options;

        await using var context = new PortalSubastasContext(options);

        context.TCatalogosBiens.AddRange(new List<TCatalogosBien>
        {
            new() { IdItem = 1, Codigo = "BIEN001", NItem = "Zebra", IdVigencia = 1, FecBaja = null },
            new() { IdItem = 2, Codigo = "BIEN002", NItem = "Alpha", IdVigencia = 1, FecBaja = null },
            new() { IdItem = 3, Codigo = "BIEN003", NItem = "Middle", IdVigencia = 1, FecBaja = null }
        });
        await context.SaveChangesAsync();

        var service = new CatalogoBienService(context, _mapper, _httpContextMock.Object, _realCache);
        var filtros = new CatalogoBienFilterDto();

        var result = await service.GetByFilterAsync(filtros);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data![0].NItem.Should().Be("Alpha");
        result.Data![1].NItem.Should().Be("Middle");
        result.Data![2].NItem.Should().Be("Zebra");
    }
}

public class ReservaDetalleServiceTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IHttpContextAccessor> _httpContextMock;
    private readonly MemoryCache _realCache;

    public ReservaDetalleServiceTests()
    {
        _mapperMock = new Mock<IMapper>();
        _httpContextMock = new Mock<IHttpContextAccessor>();
        _realCache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task CreateAsync_ReservaInexistente_ShouldReturnBadRequest()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName: "Test_CreateAsync_ReservaInexistente")
            .Options;

        await using var context = new PortalSubastasContext(options);
        var service = new ReservaDetalleService(context, _mapperMock.Object, _httpContextMock.Object, _realCache);

        var dto = new ReservaDetalleRequestDto { IdReserva = 999, IdItem = 10 };

        var result = await service.CreateAsync(dto);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(400);
        result.Message.Should().Be("La provisión no existe.");
    }

    [Fact]
    public async Task CreateAsync_ReservaAutorizada_ShouldReturnBadRequest()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName: "Test_CreateAsync_ReservaAutorizada")
            .Options;

        await using var context = new PortalSubastasContext(options);

        context.TReservas.Add(new TReserva
        {
            IdReserva = 5,
            NroReserva = "2025/000001",
            IdVigencia = 1,
            IdUnidadAdm = 1,
            IdEstado = 2,
            FechaReserva = DateOnly.FromDateTime(DateTime.Now)
        });
        await context.SaveChangesAsync();

        var service = new ReservaDetalleService(context, _mapperMock.Object, _httpContextMock.Object, _realCache);
        var dto = new ReservaDetalleRequestDto { IdReserva = 5, IdItem = 10 };

        var result = await service.CreateAsync(dto);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(400);
        result.Message.Should().Be("No se pueden modificar reservas autorizadas.");
    }

    [Fact]
    public async Task UpdateAsync_ReservaAutorizada_ShouldReturnBadRequest()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName: "Test_UpdateAsync_ReservaAutorizada")
            .Options;

        await using var context = new PortalSubastasContext(options);

        context.TReservas.Add(new TReserva
        {
            IdReserva = 5,
            NroReserva = "2025/000001",
            IdVigencia = 1,
            IdUnidadAdm = 1,
            IdEstado = 2,
            FechaReserva = DateOnly.FromDateTime(DateTime.Now)
        });
        context.TReservaDetalles.Add(new TReservaDetalle
        {
            IdReservaDet = 3,
            IdReserva = 5,
            IdItem = 10,
            IdEstado = 1
        });
        await context.SaveChangesAsync();

        var service = new ReservaDetalleService(context, _mapperMock.Object, _httpContextMock.Object, _realCache);
        var dto = new ReservaDetalleRequestDto { IdReserva = 5, IdItem = 10, Cantidad = 5, Importe = 1200 };

        var result = await service.UpdateAsync(3, dto);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(400);
        result.Message.Should().Be("No se pueden modificar reservas autorizadas.");
    }

    [Fact]
    public async Task DeleteAsync_ReservaAutorizada_ShouldReturnBadRequest()
    {
        var options = new DbContextOptionsBuilder<PortalSubastasContext>()
            .UseInMemoryDatabase(databaseName: "Test_DeleteAsync_ReservaAutorizada")
            .Options;

        await using var context = new PortalSubastasContext(options);

        context.TReservas.Add(new TReserva
        {
            IdReserva = 5,
            NroReserva = "2025/000001",
            IdVigencia = 1,
            IdUnidadAdm = 1,
            IdEstado = 2,
            FechaReserva = DateOnly.FromDateTime(DateTime.Now)
        });
        context.TReservaDetalles.Add(new TReservaDetalle
        {
            IdReservaDet = 3,
            IdReserva = 5,
            IdItem = 10,
            IdEstado = 1
        });
        await context.SaveChangesAsync();

        var service = new ReservaDetalleService(context, _mapperMock.Object, _httpContextMock.Object, _realCache);

        var result = await service.DeleteAsync(3);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(400);
        result.Message.Should().Be("No se pueden modificar reservas autorizadas.");
    }
}
