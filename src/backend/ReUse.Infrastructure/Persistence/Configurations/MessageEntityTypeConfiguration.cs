using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReUse.Domain.Entities;
using ReUse.Domain.Enums;

namespace ReUse.Infrastructure.Persistence.Configurations;

public class MessageEntityTypeConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        // PK
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
               .ValueGeneratedNever();

        // Properties
        builder.Property(m => m.MessageType)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        // Content is optional — null for Media-only messages
        builder.Property(m => m.Content)
               .HasMaxLength(4000);

        // URLs are ASCII — IsUnicode(false) saves storage
        builder.Property(m => m.MediaUrl)
               .HasMaxLength(2048)
               .IsUnicode(false);

        builder.Property(m => m.SentAt)
               .IsRequired();

        builder.Property(m => m.IsDeletedBySender)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(m => m.IsDeletedByReceiver)
               .HasDefaultValue(false)
               .IsRequired();

        // Audit
        builder.Property(m => m.CreatedAt)
               .IsRequired();

        // ── Relationships ────────────────────────────────────────────────────

        // FK → Conversation (configured from the Conversation side above via cascade)

        // FK → User (Sender) — Restrict so deleting a user does not wipe message history
        builder.HasOne(m => m.Sender)
               .WithMany()
               .HasForeignKey(m => m.SenderId)
               .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──────────────────────────────────────────────────────────

        // Paginate messages in a conversation ordered by time — the primary read pattern
        builder.HasIndex(m => new { m.ConversationId, m.SentAt });

        // "Get unread count for participant X" query
        builder.HasIndex(m => new { m.ConversationId, m.ReadAt });

        // Filter by sender within a conversation (e.g. offer-lock check)
        builder.HasIndex(m => new { m.ConversationId, m.SenderId, m.MessageType });
    }
}