
using FluentValidation;

using ReUse.Domain.Enums;

namespace ReUse.Application.DTOs.Chat.Requests;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.MessageType)
            .IsInEnum().WithMessage("Invalid message type.");

        // Text must have content
        When(x => x.MessageType == MessageType.Text, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Text messages must have content.")
                .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
        });

        // Media must have a URL
        When(x => x.MessageType == MessageType.Media, () =>
        {
            RuleFor(x => x.MediaUrl)
                .NotEmpty().WithMessage("Media messages must include a URL.")
                .MaximumLength(2048).WithMessage("Media URL is too long.");
        });

        // Offer must have a price
        When(x => x.MessageType == MessageType.Offer, () =>
        {
            RuleFor(x => x.OfferPrice)
                .NotNull().WithMessage("Offer messages must include a price.")
                .GreaterThan(0).WithMessage("Offer price must be greater than zero.");

            When(x => x.Content is not null, () =>
            {
                RuleFor(x => x.Content)
                    .MaximumLength(4000).WithMessage("Offer note cannot exceed 4000 characters.");
            });
        });

        // These types cannot be sent by a client — they are system-generated
        RuleFor(x => x.MessageType)
            .Must(t => t != MessageType.OfferAccepted &&
                       t != MessageType.OfferDeclined &&
                       t != MessageType.SystemEvent)
            .WithMessage("This message type cannot be sent directly.");
    }
}