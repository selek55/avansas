namespace Avansas.Application.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder);
    Task DeleteFileAsync(string fileUrl);
    bool IsValidImageFile(string fileName, long fileSize);
}
