using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;

public class SettingsModel : PageModel
{
    private readonly AppDbContext _db;

    public SettingsModel(AppDbContext db)
    {
        _db = db;
    }

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public string? Message { get; set; }

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is null) return RedirectToPage("/SignIn");

        Username = user.Username;
        Email = user.Email;
        IsPrivate = user.IsPrivate;
        return Page();
    }

    public async Task<IActionResult> OnPostTogglePrivacyAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is not null)
        {
            user.IsPrivate = !user.IsPrivate;
            await _db.SaveChangesAsync();
            Message = user.IsPrivate
                ? "Your account is now private - only followers can see your posts."
                : "Your account is now public.";
        }

        return await OnGetAsync();
    }
}
