using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class PostModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public PostModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public string Username { get; set; } = string.Empty;
    public PostViewModel? MainPost { get; set; }
    public List<PostViewModel> Replies { get; set; } = new();

    [BindProperty] public string ReplyContent { get; set; } = string.Empty;
    [BindProperty] public IFormFile? MediaFile { get; set; }

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username)) return RedirectToPage("/SignIn");
        Username = username;

        var loaded = await LoadAsync(id);
        if (!loaded) return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostReplyAsync(int id)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var media = await MediaUploadHelper.SaveAsync(MediaFile, _env.WebRootPath, "posts");

        if (!string.IsNullOrWhiteSpace(ReplyContent) || media is not null)
        {
            _db.Posts.Add(new Post
            {
                AuthorId = CurrentUserId.Value,
                Content = ReplyContent?.Trim() ?? string.Empty,
                ParentPostId = id,
                MediaUrl = media?.Url,
                MediaType = media?.MediaType
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Post", new { id });
    }

    public async Task<IActionResult> OnPostToggleLikeAsync(int postId, int id)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        await PostService.ToggleLikeAsync(_db, postId, CurrentUserId.Value);
        return RedirectToPage("/Post", new { id });
    }

    public async Task<IActionResult> OnPostToggleRepostAsync(int postId, int id)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        await PostService.ToggleRepostAsync(_db, postId, CurrentUserId.Value);
        return RedirectToPage("/Post", new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var post = await _db.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return false;

        post.ViewCount++;
        await _db.SaveChangesAsync();

        MainPost = await PostService.ToViewModelAsync(_db, post, CurrentUserId);

        var replies = await _db.Posts
            .Include(p => p.Author)
            .Where(p => p.ParentPostId == id)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        Replies = new List<PostViewModel>();
        foreach (var reply in replies)
        {
            Replies.Add(await PostService.ToViewModelAsync(_db, reply, CurrentUserId));
        }

        return true;
    }
}
