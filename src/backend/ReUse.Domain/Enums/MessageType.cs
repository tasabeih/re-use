namespace ReUse.Domain.Enums;

public enum MessageType
{
    Text,           // Plain text message
    Media,          // Image or file — URL provided after Cloudinary upload
    Offer,          // WantedOffer only: seller sends a price offer to the buyer
    OfferAccepted,  // System-generated when buyer accepts the offer
    OfferDeclined,  // System-generated when buyer declines the offer
    SystemEvent     // System-generated: e.g. "conversation opened", "product sold"
}