namespace Simple_Chat.Models;

public class Post
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; } // "image" or "video"

    // Null for a top-level post; set when this post is a reply to another post
    public int? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }

    public int ViewCount { get; set; }
}
