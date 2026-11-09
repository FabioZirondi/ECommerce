using FluentValidation;

namespace ECommerce.Modules.Users.Application.Validators;

public sealed class UpdateAddressValidator : AbstractValidator<DTOs.UpdateAddressRequest>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.ZipCode)
            .Must(zip => Digits(zip).Length == 8)
            .WithMessage("Informe um CEP com 8 dígitos.");
    }

    internal static string Digits(string? value)
        => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
