namespace Diyalo.Api.Services;

// Handles saving uploaded image files to wwwroot/uploads/
public class FileUploadService
{
    private readonly IWebHostEnvironment _env;

    public FileUploadService(IWebHostEnvironment env) => _env = env;

    private string GetWebRootPath()
    {
        // In dev, WebRootPath may be null if wwwroot doesn't exist — use ContentRootPath/wwwroot
        var root = _env.WebRootPath;
        if (string.IsNullOrEmpty(root))
        {
            root = Path.Combine(_env.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(root);
        }
        return root;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        // Validate file is an image
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Only image files are allowed.");

        // Create folder if it doesn't exist
        var uploadPath = Path.Combine(GetWebRootPath(), "uploads", folder);
        Directory.CreateDirectory(uploadPath);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadPath, fileName);

        // Save file
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Return public URL
        return $"/uploads/{folder}/{fileName}";
    }

    public void Delete(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;
        var filePath = Path.Combine(GetWebRootPath(), imageUrl.TrimStart('/'));
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
