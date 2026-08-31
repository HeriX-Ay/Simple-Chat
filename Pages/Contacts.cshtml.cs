using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class ContactsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public ContactsModel(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [BindProperty] public string SearchUsername { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public List<(string Username, string? AvatarUrl)> Friends { get; set; } = new();
    public List<(int FriendshipId, string Username)> IncomingRequests { get; set; } = new();
    public List<string> OutgoingRequests { get; set; } = new();

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        await LoadListsAsync(CurrentUserId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAddFriendAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        var myId = CurrentUserId.Value;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == SearchUsername.Trim());

        if (target is null)
        {
            Message = "No user found with that username.";
        }
        else if (target.Id == myId)
        {
            Message = "You can't add yourself.";
        }
        else
        {
            var existing = await _db.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == myId && f.AddresseeId == target.Id) ||
                (f.RequesterId == target.Id && f.AddresseeId == myId));

            if (existing is not null)
            {
                Message = existing.Status == FriendStatus.Accepted
                    ? $"You're already friends with {target.Username}."
                    : "A friend request is already pending with this user.";
            }
            else
            {
                _db.Friendships.Add(new Friendship
                {
                    RequesterId = myId,
                    AddresseeId = target.Id,
                    Status = FriendStatus.Pending
                });
                await _db.SaveChangesAsync();
                Message = $"Friend request sent to {target.Username}.";

                var myUsername = HttpContext.Session.GetString("Username") ?? "Someone";
                await _hub.Clients.Group($"user_{target.Username}")
                    .SendAsync("NewFriendRequestNotification", myUsername);
            }
        }

        await LoadListsAsync(myId);
        return Page();
    }

    public async Task<IActionResult> OnPostAcceptAsync(int friendshipId)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        var myId = CurrentUserId.Value;

        var request = await _db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId && f.AddresseeId == myId);
        if (request is not null)
        {
            request.Status = FriendStatus.Accepted;
            await _db.SaveChangesAsync();
        }

        await LoadListsAsync(myId);
        return Page();
    }

    public async Task<IActionResult> OnPostDeclineAsync(int friendshipId)
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        var myId = CurrentUserId.Value;

        var request = await _db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId && f.AddresseeId == myId);
        if (request is not null)
        {
            _db.Friendships.Remove(request);
            await _db.SaveChangesAsync();
        }

        await LoadListsAsync(myId);
        return Page();
    }

    private async Task LoadListsAsync(int myId)
    {
        var accepted = await _db.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendStatus.Accepted && (f.RequesterId == myId || f.AddresseeId == myId))
            .ToListAsync();

        Friends = accepted
            .Select(f => f.RequesterId == myId ? f.Addressee : f.Requester)
            .OrderBy(u => u.Username)
            .Select(u => (u.Username, u.AvatarUrl))
            .ToList();

        var incoming = await _db.Friendships
            .Include(f => f.Requester)
            .Where(f => f.Status == FriendStatus.Pending && f.AddresseeId == myId)
            .ToListAsync();

        IncomingRequests = incoming.Select(f => (f.Id, f.Requester.Username)).ToList();

        OutgoingRequests = await _db.Friendships
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendStatus.Pending && f.RequesterId == myId)
            .Select(f => f.Addressee.Username)
            .ToListAsync();
    }
}
