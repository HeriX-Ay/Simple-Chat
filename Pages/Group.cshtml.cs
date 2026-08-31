using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;

public class GroupMessageViewModel
{
    public int Id { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
}

[IgnoreAntiforgeryToken]
public class GroupModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public GroupModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public string Username { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
    public List<GroupMessageViewModel> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var username = HttpContext.Session.GetString("Username");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (string.IsNullOrEmpty(username) || userId is null) return RedirectToPage("/SignIn");

        var isMember = await _db.GroupMembers.AnyAsync(m => m.GroupId == id && m.UserId == userId);
        if (!isMember) return RedirectToPage("/Groups");

        var group = await _db.ChatGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (group is null) return RedirectToPage("/Groups");

        Username = username;
        GroupId = id;
        GroupName = group.Name;

        var members = await _db.GroupMembers.Include(m => m.User).Where(m => m.GroupId == id).ToListAsync();
        Members = members.Select(m => m.User.Username).OrderBy(u => u).ToList();

        var messages = await _db.GroupMessages
            .Include(m => m.Sender)
            .Where(m => m.GroupId == id)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        History = messages.Select(m => new GroupMessageViewModel
        {
            Id = m.Id,
            Sender = m.Sender.Username,
            SenderAvatarUrl = m.Sender.AvatarUrl,
            Content = m.Content,
            SentAt = m.SentAt,
            MediaUrl = m.MediaUrl,
            MediaType = m.MediaType
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostUploadMediaAsync(IFormFile file)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            return Unauthorized();

        var media = await MediaUploadHelper.SaveAsync(file, _env.WebRootPath, "groups");
        if (media is null) return BadRequest(new { error = "Unsupported file or file too large (max 25MB)." });

        return new JsonResult(new { url = media.Value.Url, type = media.Value.MediaType });
    }
}
