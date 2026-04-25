using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly ISiteSettingService _siteSettingService;

    public SettingsController(ISiteSettingService siteSettingService) => _siteSettingService = siteSettingService;

    public async Task<IActionResult> EmailSettings()
    {
        var settings = await _siteSettingService.GetEmailSettingsAsync();
        return View(settings);
    }

    [HttpPost]
    public async Task<IActionResult> SaveEmailSettings(Dictionary<string, string> settings)
    {
        await _siteSettingService.SaveEmailSettingsAsync(settings);
        TempData["Success"] = "E-posta ayarları kaydedildi";
        return RedirectToAction(nameof(EmailSettings));
    }
}
