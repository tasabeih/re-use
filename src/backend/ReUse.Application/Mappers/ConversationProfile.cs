using AutoMapper;

using ReUse.Application.DTOs.Chat.Responses;
using ReUse.Domain.Entities;

namespace ReUse.Application.Mappers;

public class ConversationProfile : Profile
{
    public ConversationProfile()
    {
        // Conversation → ConversationResponse
        // Used only for StartConversation (single entity, no list)
        CreateMap<Conversation, ConversationResponse>()
            .ForMember(d => d.ProductTitle,
                opt => opt.MapFrom(s => s.Product != null ? s.Product.Title : string.Empty))
            .ForMember(d => d.ProductCoverImageUrl,
                opt => opt.MapFrom(s => s.Product != null
                    ? s.Product.ProductImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.Url)
                        .FirstOrDefault()
                    : null))
            .ForMember(d => d.ProductStatus,
                opt => opt.MapFrom(s => s.Product != null ? s.Product.Status : default))
            .ForMember(d => d.BuyerName,
                opt => opt.MapFrom(s => s.Buyer != null ? s.Buyer.FullName : string.Empty))
            .ForMember(d => d.BuyerAvatarUrl,
                opt => opt.MapFrom(s => s.Buyer != null ? s.Buyer.ProfileImageUrl : null))
            .ForMember(d => d.SellerName,
                opt => opt.MapFrom(s => s.Seller != null ? s.Seller.FullName : string.Empty))
            .ForMember(d => d.SellerAvatarUrl,
                opt => opt.MapFrom(s => s.Seller != null ? s.Seller.ProfileImageUrl : null))
            .ForMember(d => d.LastMessagePreview, opt => opt.Ignore())
            .ForMember(d => d.UnreadCount, opt => opt.Ignore());

        // ConversationProjection → ConversationResponse
        // Used for GetMyConversations — preview already computed in SQL
        CreateMap<ConversationProjection, ConversationResponse>()
            .ForMember(d => d.UnreadCount, opt => opt.Ignore());

        // Message → MessageResponse (unchanged)
        CreateMap<Message, MessageResponse>()
            .ForMember(d => d.SenderName,
                opt => opt.MapFrom(s => s.Sender != null ? s.Sender.FullName : string.Empty))
            .ForMember(d => d.SenderAvatarUrl,
                opt => opt.MapFrom(s => s.Sender != null ? s.Sender.ProfileImageUrl : null))
            .ForMember(d => d.OfferPrice,
                opt => opt.MapFrom(s =>
                    s.MessageType == Domain.Enums.MessageType.Offer
                        ? ExtractOfferPrice(s.Content)
                        : null))
            .ForMember(d => d.Content,
                opt => opt.MapFrom(s =>
                    s.MessageType == Domain.Enums.MessageType.Offer
                        ? ExtractOfferNote(s.Content)
                        : s.Content));
    }

    private static decimal? ExtractOfferPrice(string? content)
    {
        if (string.IsNullOrEmpty(content)) return null;
        var pipe = content.IndexOf('|');
        var part = pipe >= 0 ? content[..pipe] : content;
        return decimal.TryParse(part, out var v) ? v : null;
    }

    private static string? ExtractOfferNote(string? content)
    {
        if (string.IsNullOrEmpty(content)) return null;
        var pipe = content.IndexOf('|');
        return pipe >= 0 && pipe < content.Length - 1 ? content[(pipe + 1)..] : null;
    }
}