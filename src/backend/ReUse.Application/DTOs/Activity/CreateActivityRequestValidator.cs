using FluentValidation;

using ReUse.Application.DTOs.Activity;

namespace ReUse.Application.Validators.Activity;

public class CreateActivityRequestValidator : AbstractValidator<CreateActivityRequest>
{
    public CreateActivityRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Activity type is required.")
            .MaximumLength(100)
            .WithMessage("Activity type cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.Metadata)
            .MaximumLength(2000)
            .WithMessage("Metadata cannot exceed 2000 characters.")
            .When(x => x.Metadata != null);

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .When(x => x.ProductId.HasValue);
    }
}