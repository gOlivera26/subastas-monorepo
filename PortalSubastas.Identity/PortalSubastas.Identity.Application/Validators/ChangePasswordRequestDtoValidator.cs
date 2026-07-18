using FluentValidation;
using PortalSubastas.Identity.Application.RequestDto.Login;

namespace PortalSubastas.Identity.Application.Validators.Login;

public class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(x => x.PasswordActual)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.NuevaPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.")
            .NotEqual(x => x.PasswordActual).WithMessage("La nueva contraseña no puede ser igual a la anterior.");
    }
}