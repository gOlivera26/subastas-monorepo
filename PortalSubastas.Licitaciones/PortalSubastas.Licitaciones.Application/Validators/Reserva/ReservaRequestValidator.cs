using FluentValidation;
using PortalSubastas.Licitaciones.Application.RequestDto.Reserva;

namespace PortalSubastas.Licitaciones.Application.Validators.Reserva;

public class ReservaRequestValidator : AbstractValidator<ReservaRequestDto>
{
    public ReservaRequestValidator()
    {
        RuleFor(x => x.IdUnidadAdm)
            .GreaterThan(0).WithMessage("El área solicitante es requerida.");

        RuleFor(x => x.FechaReserva)
            .NotEmpty().WithMessage("La fecha de reserva es requerida.");
    }
}
