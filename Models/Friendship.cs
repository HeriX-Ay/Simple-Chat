namespace Simple_Chat.Models;

public enum FriendStatus
{
    Pending,
    Accepted
}

public class Friendship
{
    public int Id { get; set; }

    // The user who sent the friend request
    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    // The user who received the friend request
    public int AddresseeId { get; set; }
    public User Addressee { get; set; } = null!;

    public FriendStatus Status { get; set; } = FriendStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
