namespace Simple_Chat.Models;

public class PrivateMessage
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool IsDeletedForEveryone { get; set; }

    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; } // "image" or "video"

    // Optional: this message is a reply to another private message
    public int? ReplyToMessageId { get; set; }
    public PrivateMessage? ReplyToMessage { get; set; }
}
