using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

public class GroupsModel : PageModel
{
    private readonly AppDbContext _db;

    public GroupsModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty] public string NewGroupName { get; set; } = string.Empty;
    [BindProperty] public List<string> SelectedFriends { get; set; } = new();

    public List<(int Id, string Name, int MemberCount)> MyGroups { get; set; } = new();
    public List<string> Friends { get; set; } = new();

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        await LoadAsync(CurrentUserId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (CurrentUserId is null) return RedirectToPage("/SignIn");
        var myId = CurrentUserId.Value;

        if (!string.IsNullOrWhiteSpace(NewGroupName))
        {
            var group = new ChatGroup { Name = NewGroupName.Trim(), CreatedById = myId };
            _db.ChatGroups.Add(group);
            await _db.SaveChangesAsync();

            _db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = myId });

            foreach (var friendUsername in SelectedFriends.Distinct())
            {
                var friend = await _db.Users.FirstOrDefaultAsync(u => u.Username == friendUsername);
                if (friend is not null)
                {
                    _db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = friend.Id });
                }
            }
            await _db.SaveChangesAsync();

            return RedirectToPage("/Group", new { id = group.Id });
        }

        await LoadAsync(myId);
        return Page();
    }

    private async Task LoadAsync(int myId)
    {
        var memberships = await _db.GroupMembers
            .Include(m => m.Group)
            .Where(m => m.UserId == myId)
            .ToListAsync();

        MyGroups = new List<(int, string, int)>();
        foreach (var membership in memberships)
        {
            var memberCount = await _db.GroupMembers.CountAsync(m => m.GroupId == membership.GroupId);
            MyGroups.Add((membership.GroupId, membership.Group.Name, memberCount));
        }

        var friendships = await _db.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendStatus.Accepted && (f.RequesterId == myId || f.AddresseeId == myId))
            .ToListAsync();

        Friends = friendships
            .Select(f => f.RequesterId == myId ? f.Addressee.Username : f.Requester.Username)
            .OrderBy(u => u)
            .ToList();
    }
}
