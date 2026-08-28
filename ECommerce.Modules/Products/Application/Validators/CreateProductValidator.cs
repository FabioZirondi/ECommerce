using FluentValidation;

namespace ECommerce.Modules.Products.Application.Validators;

public sealed class CreateProductValidator : AbstractValidator<DTOs.CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.State).MaximumLength(2);
        RuleFor(x => x.Neighborhood).MaximumLength(80);
        RuleFor(x => x.Street).MaximumLength(140);
        RuleFor(x => x.ZipCode).MaximumLength(10);
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Selecione uma categoria.");
        RuleFor(x => x.AvailableUnits).InclusiveBetween(1, 9999);
    }
}
