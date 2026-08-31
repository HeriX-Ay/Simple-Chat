using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;

public class SignInModel : PageModel
{
    private readonly AppDbContext _db;

    public SignInModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Please enter both a username and password.";
            return Page();
        }

        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == Username.Trim());

            // Deliberately the same generic message whether the username doesn't
            // exist or the password is wrong - this stops someone from probing
            // which usernames are real accounts by watching for different errors.
            if (user is null || !PasswordHasher.Verify(Password, user.PasswordHash, user.PasswordSalt))
            {
                Message = "Invalid username or password.";
                return Page();
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("UserId", user.Id);
            return RedirectToPage("/Index");
        }
        catch (Exception)
        {
            Message = "Something went wrong signing you in. Please try again.";
            return Page();
        }
    }
}
