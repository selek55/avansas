using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class SiteSettingService : ISiteSettingService
{
    private readonly IUnitOfWork _uow;
    public SiteSettingService(IUnitOfWork uow) => _uow = uow;

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _uow.SiteSettings.Query()
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, string? description = null, string category = "General")
    {
        var setting = await _uow.SiteSettings.Query()
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);

        if (setting != null)
        {
            setting.Value = value;
            if (description != null) setting.Description = description;
            setting.Category = category;
            setting.UpdatedAt = DateTime.UtcNow;
            _uow.SiteSettings.Update(setting);
        }
        else
        {
            await _uow.SiteSettings.AddAsync(new SiteSetting
            {
                Key = key, Value = value, Description = description, Category = category
            });
        }
        await _uow.SaveChangesAsync();
    }

    public async Task<List<SiteSettingDto>> GetByCategoryAsync(string category)
    {
        var settings = await _uow.SiteSettings.Query()
            .Where(s => s.Category == category && !s.IsDeleted)
            .OrderBy(s => s.Key).ToListAsync();
        return settings.Select(MapToDto).ToList();
    }

    public async Task<List<SiteSettingDto>> GetAllAsync()
    {
        var settings = await _uow.SiteSettings.Query()
            .Where(s => !s.IsDeleted).OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync();
        return settings.Select(MapToDto).ToList();
    }

    public async Task<Dictionary<string, string>> GetEmailSettingsAsync()
    {
        var settings = await _uow.SiteSettings.Query()
            .Where(s => s.Category == "Email" && !s.IsDeleted).ToListAsync();
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task SaveEmailSettingsAsync(Dictionary<string, string> settings)
    {
        foreach (var kvp in settings)
            await SetValueAsync(kvp.Key, kvp.Value, category: "Email");
    }

    private static SiteSettingDto MapToDto(SiteSetting s) => new()
    {
        Id = s.Id, Key = s.Key, Value = s.Value,
        Description = s.Description, Category = s.Category
    };
}
