namespace Simple_Chat.Models;

public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public ChatGroup Group { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
