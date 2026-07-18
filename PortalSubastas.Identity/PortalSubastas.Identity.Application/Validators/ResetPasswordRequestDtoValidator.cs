using FluentValidation;
using PortalSubastas.Identity.Application.RequestDto.Login;

namespace PortalSubastas.Identity.Application.Validators.Login;

public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio.");

        RuleFor(x => x.NuevaPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
    }
}