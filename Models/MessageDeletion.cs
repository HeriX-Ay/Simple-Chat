namespace Simple_Chat.Models;

// Records that a specific user has hidden a message from their own view
// ("delete for me"), without affecting what the other person sees.
public class MessageDeletion
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public PrivateMessage Message { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
