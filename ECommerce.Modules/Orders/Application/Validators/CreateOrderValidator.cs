using FluentValidation;

namespace ECommerce.Modules.Orders.Application.Validators;

public sealed class CreateOrderValidator : AbstractValidator<DTOs.CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x).NotNull();
    }
}
