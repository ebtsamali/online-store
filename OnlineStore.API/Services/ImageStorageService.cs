using Microsoft.AspNetCore.Http;

namespace OnlineStore.API.Services;

public interface IImageStorageService
{
    /// <summary>Validates the file; throws InvalidImageException (→ 400) on failure. No-op on success.</summary>
    void Validate(IFormFile file);

    /// <summary>Writes the file under wwwroot/images/products and returns the public relative URL (e.g. /images/products/{guid}.png).</summary>
    Task<string> SaveAsync(IFormFile file);

    /// <summary>Deletes a previously-saved file given its public relative URL. Safe to call if the file is missing.</summary>
    void Delete(string relativeUrl);
}

public class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}

public class ImageStorageService : IImageStorageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private const string RelativeDir = "images/products";

    private readonly IWebHostEnvironment _env;

    public ImageStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public void Validate(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new InvalidImageException("Image is required.");

        if (file.Length > MaxBytes)
            throw new InvalidImageException("Image must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidImageException("Image must be a .jpg, .jpeg, .png, or .webp file.");
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        // WebRootPath is null until wwwroot exists — ensure the folder.
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var targetDir = Path.Combine(webRoot, "images", "products");
        Directory.CreateDirectory(targetDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{RelativeDir}/{fileName}";
    }

    public void Delete(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
