namespace ReUse.Domain.Enums;

public enum ConversationStatus
{
    Active,
    Closed,           // Manually closed by either participant
    Archived,         // Soft-hidden by a participant; still readable
    InactivityClosed  // Auto-closed by background job after 30 days of no activity
}