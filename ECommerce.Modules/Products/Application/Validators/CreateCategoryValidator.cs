using FluentValidation;

namespace ECommerce.Modules.Products.Application.Validators;

public sealed class CreateCategoryValidator : AbstractValidator<DTOs.CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(60);
        RuleFor(x => x.ParentId).MaximumLength(80);
    }
}
