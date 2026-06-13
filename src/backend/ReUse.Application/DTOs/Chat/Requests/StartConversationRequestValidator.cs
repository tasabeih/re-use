using FluentValidation;
namespace ReUse.Application.DTOs.Chat.Requests;

public class StartConversationRequestValidator : AbstractValidator<StartConversationRequest>
{
    public StartConversationRequestValidator()
    {
        When(x => x.InitialMessage is not null, () =>
        {
            RuleFor(x => x.InitialMessage)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
        });
    }
}