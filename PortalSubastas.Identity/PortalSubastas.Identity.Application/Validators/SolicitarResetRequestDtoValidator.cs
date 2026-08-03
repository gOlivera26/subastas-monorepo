using FluentValidation;
using PortalSubastas.Identity.Application.RequestDto.Login;

namespace PortalSubastas.Identity.Application.Validators.Login;

public class SolicitarResetRequestDtoValidator : AbstractValidator<SolicitarResetRequestDto>
{
    public SolicitarResetRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");
    }
}