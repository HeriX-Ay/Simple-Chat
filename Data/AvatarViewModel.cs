namespace Simple_Chat.Data;

public class AvatarViewModel
{
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string SizeClass { get; set; } = "avatar-md";

    public AvatarViewModel(string username, string? avatarUrl, string sizeClass = "avatar-md")
    {
        Username = username;
        AvatarUrl = avatarUrl;
        SizeClass = sizeClass;
    }
}
