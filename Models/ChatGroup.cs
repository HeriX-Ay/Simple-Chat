namespace Simple_Chat.Models;

public class ChatGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
