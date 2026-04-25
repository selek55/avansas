using Avansas.Application.Interfaces;
using Avansas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ReturnsController : Controller
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService) => _returnService = returnService;

    public async Task<IActionResult> Index(ReturnStatus? status = null)
    {
        var returns = await _returnService.GetAllReturnsAsync(status);
        ViewBag.CurrentStatus = status;
        return View(returns);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var ret = await _returnService.GetReturnByIdAsync(id);
        if (ret == null) return NotFound();
        return View(ret);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? adminNotes)
    {
        await _returnService.ApproveReturnAsync(id, adminNotes);
        TempData["Success"] = "İade talebi onaylandı.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? adminNotes)
    {
        await _returnService.RejectReturnAsync(id, adminNotes);
        TempData["Success"] = "İade talebi reddedildi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int id)
    {
        await _returnService.RefundReturnAsync(id);
        TempData["Success"] = "İade tutarı iade edildi.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
