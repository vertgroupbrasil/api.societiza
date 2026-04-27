using FluentValidation;
using Identity.Shared.Queries.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class GetUserByIdValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
