using FluentValidation;
using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;

namespace PortalSubastas.Licitaciones.Application.Validators.ReservaDetalle;

public class ReservaDetalleRequestValidator : AbstractValidator<ReservaDetalleRequestDto>
{
    public ReservaDetalleRequestValidator()
    {
        RuleFor(x => x.IdReserva)
            .GreaterThan(0).WithMessage("La provisión es requerida.");

        RuleFor(x => x.IdItem)
            .GreaterThan(0).WithMessage("El bien o servicio es requerido.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).When(x => x.Cantidad.HasValue).WithMessage("La cantidad debe ser mayor a cero.");

        RuleFor(x => x.Importe)
            .GreaterThanOrEqualTo(0).When(x => x.Importe.HasValue).WithMessage("El importe sugerido debe ser mayor o igual a cero.");
    }
}
