using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class SignUpModel : PageModel
{
    private readonly AppDbContext _db;

    public SignUpModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    [Required(ErrorMessage = "Please choose a username.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be 3-20 characters.")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Please confirm your password.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty] public string? Bio { get; set; }

    public string Message { get; set; } = string.Empty;

    private static readonly Regex UsernamePattern = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public async Task<IActionResult> OnPostAsync()
    {
        // Run the [Required]/[StringLength]/[EmailAddress] attribute checks above.
        // This never leaks exception details to the page - just field-level messages.
        if (!ModelState.IsValid)
        {
            Message = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Please check the form and try again.";
            return Page();
        }

        Username = Username.Trim();
        Email = Email.Trim();

        if (!UsernamePattern.IsMatch(Username))
        {
            Message = "Username can only contain letters, numbers, and underscores.";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            Message = "Passwords don't match.";
            return Page();
        }

        try
        {
            var usernameTaken = await _db.Users.AnyAsync(u => u.Username == Username);
            if (usernameTaken)
            {
                Message = "That username is already taken.";
                return Page();
            }

            var emailTaken = await _db.Users.AnyAsync(u => u.Email == Email);
            if (emailTaken)
            {
                Message = "An account with that email already exists.";
                return Page();
            }

            var (hash, salt) = PasswordHasher.Hash(Password);
            _db.Users.Add(new User
            {
                Username = Username,
                Email = Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim()
            });
            await _db.SaveChangesAsync();

            return RedirectToPage("/SignIn");
        }
        catch (Exception)
        {
            // Never show the real exception to the user - log it server-side in a
            // real deployment, but keep the message generic here.
            Message = "Something went wrong creating your account. Please try again.";
            return Page();
        }
    }
}
