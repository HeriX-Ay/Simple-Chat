using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class UserProfileModel : PageModel
{
    private readonly AppDbContext _db;

    public UserProfileModel(AppDbContext db)
    {
        _db = db;
    }

    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsOwnProfile { get; set; }
    public bool IsFollowing { get; set; }
    public bool CanViewContent { get; set; }

    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }

    public string ActiveTab { get; set; } = "posts";
    public List<PostViewModel> Posts { get; set; } = new();
    public List<PostViewModel> Reposts { get; set; } = new();
    public List<PostViewModel> Likes { get; set; } = new();

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync(string username, string? tab)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            return RedirectToPage("/SignIn");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (target is null) return RedirectToPage("/Index");

        Username = target.Username;
        AvatarUrl = target.AvatarUrl;
        Bio = target.Bio;
        IsPrivate = target.IsPrivate;
        IsOwnProfile = CurrentUserId == target.Id;

        FollowerCount = await _db.Follows.CountAsync(f => f.FollowingId == target.Id);
        FollowingCount = await _db.Follows.CountAsync(f => f.FollowerId == target.Id);
        PostCount = await _db.Posts.CountAsync(p => p.AuthorId == target.Id && p.ParentPostId == null);

        IsFollowing = CurrentUserId is not null &&
            await _db.Follows.AnyAsync(f => f.FollowerId == CurrentUserId && f.FollowingId == target.Id);

        // A private account's posts are only visible to the owner and their followers.
        CanViewContent = IsOwnProfile || !IsPrivate || IsFollowing;

        ActiveTab = tab is "reposts" or "likes" ? tab : "posts";

        if (CanViewContent)
        {
            switch (ActiveTab)
            {
                case "reposts":
                    Reposts = await PostService.GetRepostsByUserAsync(_db, target.Id, CurrentUserId);
                    break;
                case "likes":
                    Likes = await PostService.GetLikedPostsByUserAsync(_db, target.Id, CurrentUserId);
                    break;
                default:
                    Posts = await PostService.GetPostsByUserAsync(_db, target.Id, CurrentUserId);
                    break;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleFollowAsync(string username)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (target is not null)
        {
            await PostService.ToggleFollowAsync(_db, CurrentUserId.Value, target.Id);
        }

        return RedirectToPage("/UserProfile", new { username });
    }
}
