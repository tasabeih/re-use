using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReUse.Domain.Entities;
using ReUse.Domain.Enums;

namespace ReUse.Infrastructure.Persistence.Configurations;

public class ConversationEntityTypeConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        // PK
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
               .ValueGeneratedNever();

        // Properties
        builder.Property(c => c.ConversationType)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(c => c.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired()
               .HasDefaultValue(ConversationStatus.Active);

        builder.Property(c => c.IsActive)
               .HasDefaultValue(true)
               .IsRequired();

        builder.Property(c => c.LastActivityAt)
               .IsRequired();

        // Audit
        builder.Property(c => c.CreatedAt)
               .IsRequired();

        // ── Relationships ────────────────────────────────────────────────────

        // FK → Product (Restrict so deleting a product does not cascade-wipe conversations)
        builder.HasOne(c => c.Product)
               .WithMany()
               .HasForeignKey(c => c.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        // FK → User (Buyer)
        builder.HasOne(c => c.Buyer)
               .WithMany()
               .HasForeignKey(c => c.BuyerId)
               .OnDelete(DeleteBehavior.Restrict);

        // FK → User (Seller)
        builder.HasOne(c => c.Seller)
               .WithMany()
               .HasForeignKey(c => c.SellerId)
               .OnDelete(DeleteBehavior.Restrict);

        // One-to-many → Messages (cascade so deleting a conversation wipes its messages)
        builder.HasMany(c => c.Messages)
               .WithOne(m => m.Conversation)
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);

        // ── Constraints ──────────────────────────────────────────────────────

        // One thread per (product, buyer, seller) triplet — prevents duplicates
        builder.HasIndex(c => new { c.ProductId, c.BuyerId, c.SellerId })
               .IsUnique();

        // A user cannot be both buyer and seller in the same conversation
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Conversation_Buyer_Not_Seller",
            "\"BuyerId\" <> \"SellerId\""));

        // ── Indexes ──────────────────────────────────────────────────────────

        // "Get all conversations for user X" — the most common query
        builder.HasIndex(c => new { c.BuyerId, c.Status });
        builder.HasIndex(c => new { c.SellerId, c.Status });

        // Used by the background job to find stale conversations
        builder.HasIndex(c => c.LastActivityAt);

        // Used when checking if a conversation exists for a product
        builder.HasIndex(c => c.ProductId);
    }
}