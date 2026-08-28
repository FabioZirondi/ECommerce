using FluentValidation;

namespace ECommerce.Modules.Products.Application.Validators;

public sealed class UpdateProductValidator : AbstractValidator<DTOs.UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AvailableUnits.HasValue || !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Informe as unidades ou o status do anúncio.");

        RuleFor(x => x.AvailableUnits)
            .InclusiveBetween(0, 9999)
            .When(x => x.AvailableUnits.HasValue);

        RuleFor(x => x.Status)
            .Must(status => !string.IsNullOrWhiteSpace(status)
                            && (status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                                || status.Equals("Closed", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Status deve ser Active ou Closed.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }
}
