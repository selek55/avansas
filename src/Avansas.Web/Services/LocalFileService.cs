using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Avansas.Web.Services;

public class LocalFileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public LocalFileService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(_env.WebRootPath, folder);

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, uniqueName);
        await using var fs = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fs);

        return $"/{folder.Replace('\\', '/')}/{uniqueName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public bool IsValidImageFile(string fileName, long fileSize)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext) && fileSize <= MaxFileSize;
    }
}
