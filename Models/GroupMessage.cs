namespace Simple_Chat.Models;

public class GroupMessage
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public ChatGroup Group { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; } // "image" or "video"
}
