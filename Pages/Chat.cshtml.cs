using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;

public class ChatMessageViewModel
{
    public int Id { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeletedForEveryone { get; set; }
    public string? ReplyToAuthor { get; set; }
    public string? ReplyToSnippet { get; set; }
    public int? ReplyToMessageId { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
}

[IgnoreAntiforgeryToken]
public class ChatModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ChatModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> OnPostUploadMediaAsync(IFormFile file)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            return Unauthorized();

        var media = await MediaUploadHelper.SaveAsync(file, _env.WebRootPath, "messages");
        if (media is null) return BadRequest(new { error = "Unsupported file or file too large (max 25MB)." });

        return new JsonResult(new { url = media.Value.Url, type = media.Value.MediaType });
    }

    public string Username { get; set; } = string.Empty;
    public string With { get; set; } = string.Empty;
    public string? WithAvatarUrl { get; set; }
    public bool IsFriend { get; set; }

    public List<ChatMessageViewModel> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string with)
    {
        var username = HttpContext.Session.GetString("Username");
        var userId = HttpContext.Session.GetInt32("UserId");

        if (string.IsNullOrEmpty(username) || userId is null)
            return RedirectToPage("/SignIn");

        With = with;
        Username = username;

        var otherUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == with);
        if (otherUser is null)
            return RedirectToPage("/Contacts");

        WithAvatarUrl = otherUser.AvatarUrl;

        IsFriend = await _db.Friendships.AnyAsync(f =>
            f.Status == Simple_Chat.Models.FriendStatus.Accepted &&
            ((f.RequesterId == userId && f.AddresseeId == otherUser.Id) ||
             (f.RequesterId == otherUser.Id && f.AddresseeId == userId)));

        if (!IsFriend)
            return RedirectToPage("/Contacts");

        var hiddenIds = await _db.MessageDeletions
            .Where(d => d.UserId == userId)
            .Select(d => d.MessageId)
            .ToListAsync();

        var messages = await _db.PrivateMessages
            .Where(m =>
                (m.SenderId == userId && m.ReceiverId == otherUser.Id) ||
                (m.SenderId == otherUser.Id && m.ReceiverId == userId))
            .Where(m => !hiddenIds.Contains(m.Id))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        var replyIds = messages.Where(m => m.ReplyToMessageId != null).Select(m => m.ReplyToMessageId!.Value).ToList();
        var replySources = await _db.PrivateMessages
            .Where(m => replyIds.Contains(m.Id))
            .ToListAsync();

        History = messages.Select(m =>
        {
            var vm = new ChatMessageViewModel
            {
                Id = m.Id,
                Sender = m.SenderId == userId ? username : with,
                Content = m.IsDeletedForEveryone ? "This message was deleted" : m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                IsDeletedForEveryone = m.IsDeletedForEveryone,
                ReplyToMessageId = m.ReplyToMessageId,
                MediaUrl = m.IsDeletedForEveryone ? null : m.MediaUrl,
                MediaType = m.IsDeletedForEveryone ? null : m.MediaType
            };

            if (m.ReplyToMessageId is not null)
            {
                var source = replySources.FirstOrDefault(s => s.Id == m.ReplyToMessageId);
                if (source is not null)
                {
                    vm.ReplyToAuthor = source.SenderId == userId ? username : with;
                    vm.ReplyToSnippet = source.IsDeletedForEveryone
                        ? "message was deleted"
                        : (source.Content.Length > 60 ? source.Content[..60] + "…" : source.Content);
                }
            }

            return vm;
        }).ToList();

        return Page();
    }
}
