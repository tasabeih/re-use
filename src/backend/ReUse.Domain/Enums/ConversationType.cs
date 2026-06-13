namespace ReUse.Domain.Enums;

public enum ConversationType
{
    BuyerSeller,  // Regular product  — buyer contacts the seller
    WantedOffer,  // Wanted product   — seller sends an offer to the poster (buyer)
    SwapRequest   // Swap product     — user proposes a swap to the product owner
}