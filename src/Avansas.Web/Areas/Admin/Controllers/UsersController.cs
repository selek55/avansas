using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        var result = await _userService.GetUsersAsync(page, 20, search);
        ViewBag.Search = search;
        return View(result);
    }

    public async Task<IActionResult> Detail(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(string userId, bool isActive)
    {
        await _userService.SetUserActiveStatusAsync(userId, isActive);
        return Json(new { success = true });
    }
}
