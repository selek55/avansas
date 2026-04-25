using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface ISiteSettingService
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, string? description = null, string category = "General");
    Task<List<SiteSettingDto>> GetByCategoryAsync(string category);
    Task<List<SiteSettingDto>> GetAllAsync();
    Task<Dictionary<string, string>> GetEmailSettingsAsync();
    Task SaveEmailSettingsAsync(Dictionary<string, string> settings);
}
