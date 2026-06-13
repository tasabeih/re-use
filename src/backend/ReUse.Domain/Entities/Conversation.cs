using ReUse.Domain.Enums;

namespace ReUse.Domain.Entities;

public class Conversation : BaseEntity
{
    // The listing this conversation is anchored to.
    // A conversation can ONLY be started from a product — never from a user profile.
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    // Role mapping per ConversationType:
    //
    //   BuyerSeller  → BuyerId = person who contacts the owner
    //                  SellerId = product owner
    //
    //   WantedOffer  → BuyerId = product owner (the one who posted "I want X")
    //                  SellerId = person making the offer (has the item)
    //
    //   SwapRequest  → BuyerId = person proposing the swap
    //                  SellerId = product owner
    //
    // Roles are intentionally named Buyer/Seller to reflect the financial direction,
    // not necessarily who initiated the conversation.

    public Guid BuyerId { get; set; }
    public User Buyer { get; set; } = default!;

    public Guid SellerId { get; set; }
    public User Seller { get; set; } = default!;

    public ConversationType ConversationType { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;

    // IsActive is derived from Status but kept as a separate flag for fast filtering.
    // Set to false whenever Status changes to anything other than Active.
    public bool IsActive { get; set; } = true;

    // Updated on every message sent.
    // The background job reads this to auto-close after 30 days of inactivity.
    public DateTime LastActivityAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}