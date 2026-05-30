using FluentValidation.TestHelper;
using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.Validators.ReservaDetalle;

namespace PortalSubastas.Licitaciones.Tests.Unit.Validators;

public class ReservaDetalleRequestValidatorTests
{
    private readonly ReservaDetalleRequestValidator _validator = new();

    [Fact]
    public void Importe_NegativeValue_ShouldFailValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Importe = -1 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Importe);
    }

    [Fact]
    public void Importe_Zero_ShouldPassValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Importe = 0 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Importe);
    }

    [Fact]
    public void Importe_Null_ShouldPassValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Importe = null };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Importe);
    }

    [Fact]
    public void Importe_PositiveValue_ShouldPassValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Importe = 100 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Importe);
    }

    [Fact]
    public void Cantidad_NegativeValue_ShouldFailValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Cantidad = -1 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Cantidad);
    }

    [Fact]
    public void Cantidad_Zero_ShouldFailValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Cantidad = 0 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Cantidad);
    }

    [Fact]
    public void Cantidad_Null_ShouldPassValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Cantidad = null };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Cantidad);
    }

    [Fact]
    public void Cantidad_PositiveValue_ShouldPassValidation()
    {
        var dto = new ReservaDetalleRequestDto { IdReserva = 1, IdItem = 1, Cantidad = 1 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Cantidad);
    }
}
