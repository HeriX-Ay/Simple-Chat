using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public IndexModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public string Username { get; set; } = string.Empty;

    [BindProperty] public string NewPostContent { get; set; } = string.Empty;
    [BindProperty] public IFormFile? MediaFile { get; set; }
    public List<PostViewModel> Feed { get; set; } = new();

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username)) return RedirectToPage("/SignIn");
        Username = username;

        await LoadFeedAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostComposeAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var media = await MediaUploadHelper.SaveAsync(MediaFile, _env.WebRootPath, "posts");

        if (!string.IsNullOrWhiteSpace(NewPostContent) || media is not null)
        {
            _db.Posts.Add(new Post
            {
                AuthorId = CurrentUserId.Value,
                Content = NewPostContent?.Trim() ?? string.Empty,
                ParentPostId = null,
                MediaUrl = media?.Url,
                MediaType = media?.MediaType
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostToggleLikeAsync(int postId)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        await PostService.ToggleLikeAsync(_db, postId, CurrentUserId.Value);
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostToggleRepostAsync(int postId)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        await PostService.ToggleRepostAsync(_db, postId, CurrentUserId.Value);
        return RedirectToPage("/Index");
    }

    private async Task LoadFeedAsync()
    {
        Username = HttpContext.Session.GetString("Username") ?? string.Empty;

        var topLevelPosts = await _db.Posts
            .Include(p => p.Author)
            .Where(p => p.ParentPostId == null)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToListAsync();

        var items = new List<PostViewModel>();
        foreach (var post in topLevelPosts)
        {
            items.Add(await PostService.ToViewModelAsync(_db, post, CurrentUserId));
        }

        // Fold in reposts so they show up in the timeline as "X reposted"
        var recentReposts = await _db.Reposts
            .Include(r => r.User)
            .Include(r => r.Post).ThenInclude(p => p.Author)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync();

        foreach (var repost in recentReposts)
        {
            var vm = await PostService.ToViewModelAsync(_db, repost.Post, CurrentUserId, repost.User.Username);
            vm.SortTime = repost.CreatedAt;
            items.Add(vm);
        }

        Feed = items.OrderByDescending(i => i.SortTime).ToList();
    }
}
