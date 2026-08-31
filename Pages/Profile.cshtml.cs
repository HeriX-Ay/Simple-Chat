using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfileModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Message { get; set; }

    [BindProperty] public IFormFile? AvatarFile { get; set; }
    [BindProperty] public string? Bio { get; set; }

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is null) return RedirectToPage("/SignIn");

        Username = user.Username;
        AvatarUrl = user.AvatarUrl;
        Bio = user.Bio;
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is null) return RedirectToPage("/SignIn");

        var media = await MediaUploadHelper.SaveAsync(AvatarFile, _env.WebRootPath, "avatars");
        if (media is null)
        {
            Message = "Please choose a valid image (jpg, png, gif, or webp) under 25MB.";
            Username = user.Username;
            AvatarUrl = user.AvatarUrl;
            Bio = user.Bio;
            return Page();
        }

        user.AvatarUrl = media.Value.Url;
        await _db.SaveChangesAsync();

        return RedirectToPage("/Profile");
    }

    public async Task<IActionResult> OnPostRemoveAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is not null)
        {
            user.AvatarUrl = null;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Profile");
    }

    public async Task<IActionResult> OnPostSaveBioAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is not null)
        {
            var trimmed = Bio?.Trim();
            user.Bio = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed.Length > 150 ? trimmed[..150] : trimmed;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Profile");
    }
}
