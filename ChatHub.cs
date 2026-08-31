using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    // ---------- Live notifications (badge updates without refreshing) ----------

    private static string UserNotificationGroup(string username) => $"user_{username}";

    public async Task JoinUserNotifications(string username)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserNotificationGroup(username));
    }

    // ---------- Private 1:1 chat ----------

    private static string PrivateRoomName(string userA, string userB)
    {
        var pair = new[] { userA, userB };
        Array.Sort(pair, StringComparer.OrdinalIgnoreCase);
        return $"private_{pair[0]}_{pair[1]}";
    }

    public async Task JoinPrivateRoom(string currentUser, string otherUser)
    {
        var room = PrivateRoomName(currentUser, otherUser);
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
    }

    public async Task SendPrivateMessage(string fromUser, string toUser, string message, int? replyToMessageId, string? mediaUrl, string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrEmpty(mediaUrl)) return;

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Username == fromUser);
        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Username == toUser);
        if (sender is null || receiver is null) return;

        PrivateMessage? replyTo = null;
        if (replyToMessageId is not null)
        {
            replyTo = await _db.PrivateMessages.FirstOrDefaultAsync(m => m.Id == replyToMessageId);
        }

        var newMessage = new PrivateMessage
        {
            SenderId = sender.Id,
            ReceiverId = receiver.Id,
            Content = message ?? string.Empty,
            ReplyToMessageId = replyTo?.Id,
            MediaUrl = mediaUrl,
            MediaType = mediaType
        };
        _db.PrivateMessages.Add(newMessage);
        await _db.SaveChangesAsync();

        string? replyAuthor = null;
        string? replySnippet = null;
        if (replyTo is not null)
        {
            var replyAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == replyTo.SenderId);
            replyAuthor = replyAuthorUser?.Username;
            replySnippet = replyTo.IsDeletedForEveryone
                ? "message was deleted"
                : (replyTo.Content.Length > 60 ? replyTo.Content[..60] + "…" : replyTo.Content);
        }

        var room = PrivateRoomName(fromUser, toUser);
        await Clients.Group(room).SendAsync(
            "ReceivePrivateMessage", fromUser, message, newMessage.Id, replyTo?.Id, replyAuthor, replySnippet, mediaUrl, mediaType);

        // Let the receiver's badge update live even if they're not on this chat page
        await Clients.Group(UserNotificationGroup(toUser)).SendAsync("NewMessageNotification", fromUser);
    }

    public async Task DeleteForEveryone(int messageId, string requestingUser)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == requestingUser);
        var message = await _db.PrivateMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (user is null || message is null || message.SenderId != user.Id) return;

        message.IsDeletedForEveryone = true;
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == message.SenderId);
        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Id == message.ReceiverId);
        if (sender is null || receiver is null) return;

        var room = PrivateRoomName(sender.Username, receiver.Username);
        await Clients.Group(room).SendAsync("MessageDeleted", messageId);
    }

    public async Task DeleteForMe(int messageId, string requestingUser)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == requestingUser);
        if (user is null) return;

        var alreadyHidden = await _db.MessageDeletions.AnyAsync(d => d.MessageId == messageId && d.UserId == user.Id);
        if (!alreadyHidden)
        {
            _db.MessageDeletions.Add(new MessageDeletion { MessageId = messageId, UserId = user.Id });
            await _db.SaveChangesAsync();
        }

        await Clients.Caller.SendAsync("MessageRemovedForMe", messageId);
    }

    public async Task MarkMessagesRead(string readerUsername, string otherUsername)
    {
        var reader = await _db.Users.FirstOrDefaultAsync(u => u.Username == readerUsername);
        var other = await _db.Users.FirstOrDefaultAsync(u => u.Username == otherUsername);
        if (reader is null || other is null) return;

        var unread = await _db.PrivateMessages
            .Where(m => m.SenderId == other.Id && m.ReceiverId == reader.Id && !m.IsRead)
            .ToListAsync();

        if (unread.Count == 0) return;

        foreach (var msg in unread)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        var room = PrivateRoomName(readerUsername, otherUsername);
        await Clients.Group(room).SendAsync("MessagesRead", readerUsername);
    }

    // ---------- Group chat ----------

    private static string GroupRoomName(int groupId) => $"group_{groupId}";

    public async Task JoinGroupRoom(int groupId, string username)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return;

        var isMember = await _db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
        if (!isMember) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupRoomName(groupId));
    }

    public async Task SendGroupMessage(int groupId, string fromUser, string message, string? mediaUrl, string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrEmpty(mediaUrl)) return;

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Username == fromUser);
        if (sender is null) return;

        var isMember = await _db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == sender.Id);
        if (!isMember) return;

        var newMessage = new GroupMessage
        {
            GroupId = groupId,
            SenderId = sender.Id,
            Content = message ?? string.Empty,
            MediaUrl = mediaUrl,
            MediaType = mediaType
        };
        _db.GroupMessages.Add(newMessage);
        await _db.SaveChangesAsync();

        await Clients.Group(GroupRoomName(groupId)).SendAsync(
            "ReceiveGroupMessage", fromUser, message, newMessage.Id, mediaUrl, mediaType, sender.AvatarUrl);
    }

    public async Task GroupTyping(int groupId, string username)
    {
        await Clients.OthersInGroup(GroupRoomName(groupId)).SendAsync("GroupUserTyping", username);
    }

    public async Task StopGroupTyping(int groupId)
    {
        await Clients.OthersInGroup(GroupRoomName(groupId)).SendAsync("GroupStopTyping");
    }

    public async Task PrivateTyping(string fromUser, string toUser)
    {
        var room = PrivateRoomName(fromUser, toUser);
        await Clients.OthersInGroup(room).SendAsync("PrivateUserTyping", fromUser);
    }

    public async Task StopPrivateTyping(string fromUser, string toUser)
    {
        var room = PrivateRoomName(fromUser, toUser);
        await Clients.OthersInGroup(room).SendAsync("PrivateStopTyping");
    }
}
