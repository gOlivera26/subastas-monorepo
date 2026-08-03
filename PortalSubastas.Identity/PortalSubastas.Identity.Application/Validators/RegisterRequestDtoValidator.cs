using FluentValidation;
using PortalSubastas.Identity.Application.RequestDto.Login;

namespace PortalSubastas.Identity.Application.Validators.Login;

public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.Apellido).NotEmpty().WithMessage("El apellido es obligatorio.");
        RuleFor(x => x.NroDocumento).NotEmpty().WithMessage("El número de documento es obligatorio.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.IdTipoPersona).GreaterThan(0).WithMessage("El tipo de persona es inválido.");
        RuleFor(x => x.IdTipoDocumento).GreaterThan(0).WithMessage("El tipo de documento es inválido.");
    }
}