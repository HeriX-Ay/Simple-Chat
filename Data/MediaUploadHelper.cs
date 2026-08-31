namespace Simple_Chat.Data;

public static class MediaUploadHelper
{
    private static readonly Dictionary<string, string> ImageTypes = new()
    {
        [".jpg"] = "image", [".jpeg"] = "image", [".png"] = "image",
        [".gif"] = "image", [".webp"] = "image"
    };

    private static readonly Dictionary<string, string> VideoTypes = new()
    {
        [".mp4"] = "video", [".webm"] = "video", [".mov"] = "video"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    /// <summary>
    /// Saves an uploaded image/video into wwwroot/uploads/{subfolder}, returning the public
    /// URL and media type ("image" or "video"). Returns null if the file is missing, too
    /// large, or not an allowed type.
    /// </summary>
    public static async Task<(string Url, string MediaType)?> SaveAsync(IFormFile? file, string webRootPath, string subfolder)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > MaxFileSizeBytes) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        string? mediaType = ImageTypes.TryGetValue(ext, out var img) ? img
            : VideoTypes.TryGetValue(ext, out var vid) ? vid
            : null;

        if (mediaType is null) return null;

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var folderPath = Path.Combine(webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(folderPath);

        var fullPath = Path.Combine(folderPath, fileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/{subfolder}/{fileName}";
        return (url, mediaType);
    }
}
