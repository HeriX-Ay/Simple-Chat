using Microsoft.EntityFrameworkCore;
using Simple_Chat.Data;
using Simple_Chat.Models;

namespace Simple_Chat.Data;

public class PostViewModel
{
    public int Id { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? ParentPostId { get; set; }
    public string? ReplyingToUsername { get; set; }

    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public bool IsRepostedByMe { get; set; }

    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public int ViewCount { get; set; }

    // Set when this feed entry is showing up because someone reposted it
    public string? RepostedByUsername { get; set; }
    public DateTime SortTime { get; set; }
}

public static class PostService
{
    public static async Task<PostViewModel> ToViewModelAsync(AppDbContext db, Post post, int? currentUserId, string? repostedBy = null)
    {
        var likeCount = await db.PostLikes.CountAsync(l => l.PostId == post.Id);
        var replyCount = await db.Posts.CountAsync(p => p.ParentPostId == post.Id);
        var repostCount = await db.Reposts.CountAsync(r => r.PostId == post.Id);

        var isLiked = currentUserId is not null &&
            await db.PostLikes.AnyAsync(l => l.PostId == post.Id && l.UserId == currentUserId);

        var isReposted = currentUserId is not null &&
            await db.Reposts.AnyAsync(r => r.PostId == post.Id && r.UserId == currentUserId);

        string? replyingTo = null;
        if (post.ParentPostId is not null)
        {
            var parent = await db.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == post.ParentPostId);
            replyingTo = parent?.Author.Username;
        }

        return new PostViewModel
        {
            Id = post.Id,
            AuthorUsername = post.Author.Username,
            AuthorAvatarUrl = post.Author.AvatarUrl,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            ParentPostId = post.ParentPostId,
            ReplyingToUsername = replyingTo,
            LikeCount = likeCount,
            ReplyCount = replyCount,
            RepostCount = repostCount,
            IsLikedByMe = isLiked,
            IsRepostedByMe = isReposted,
            RepostedByUsername = repostedBy,
            SortTime = post.CreatedAt,
            MediaUrl = post.MediaUrl,
            MediaType = post.MediaType,
            ViewCount = post.ViewCount
        };
    }

    public static async Task ToggleLikeAsync(AppDbContext db, int postId, int userId)
    {
        var existing = await db.PostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        if (existing is not null)
        {
            db.PostLikes.Remove(existing);
        }
        else
        {
            db.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
        }
        await db.SaveChangesAsync();
    }

    public static async Task ToggleRepostAsync(AppDbContext db, int postId, int userId)
    {
        var existing = await db.Reposts.FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);
        if (existing is not null)
        {
            db.Reposts.Remove(existing);
        }
        else
        {
            db.Reposts.Add(new Repost { PostId = postId, UserId = userId });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Top-level posts authored by the given user, newest first.</summary>
    public static async Task<List<PostViewModel>> GetPostsByUserAsync(AppDbContext db, int authorId, int? currentUserId)
    {
        var posts = await db.Posts
            .Include(p => p.Author)
            .Where(p => p.AuthorId == authorId && p.ParentPostId == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<PostViewModel>();
        foreach (var post in posts)
        {
            result.Add(await ToViewModelAsync(db, post, currentUserId));
        }
        return result;
    }

    /// <summary>Posts the given user has reposted, newest repost first.</summary>
    public static async Task<List<PostViewModel>> GetRepostsByUserAsync(AppDbContext db, int userId, int? currentUserId)
    {
        var reposts = await db.Reposts
            .Include(r => r.Post).ThenInclude(p => p.Author)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<PostViewModel>();
        foreach (var repost in reposts)
        {
            result.Add(await ToViewModelAsync(db, repost.Post, currentUserId));
        }
        return result;
    }

    /// <summary>Posts the given user has liked, newest like first.</summary>
    public static async Task<List<PostViewModel>> GetLikedPostsByUserAsync(AppDbContext db, int userId, int? currentUserId)
    {
        var likes = await db.PostLikes
            .Include(l => l.Post).ThenInclude(p => p.Author)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var result = new List<PostViewModel>();
        foreach (var like in likes)
        {
            result.Add(await ToViewModelAsync(db, like.Post, currentUserId));
        }
        return result;
    }

    public static async Task ToggleFollowAsync(AppDbContext db, int followerId, int followingId)
    {
        if (followerId == followingId) return;

        var existing = await db.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        if (existing is not null)
        {
            db.Follows.Remove(existing);
        }
        else
        {
            db.Follows.Add(new Follow { FollowerId = followerId, FollowingId = followingId });
        }
        await db.SaveChangesAsync();
    }
}
