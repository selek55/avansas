using Avansas.Application.Interfaces;
using Avansas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Avansas.Domain.Entities;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TicketsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsController(ITicketService ticketService, UserManager<ApplicationUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1, TicketStatus? status = null)
    {
        var result = await _ticketService.GetAllTicketsAsync(page, 20, status);
        ViewBag.CurrentStatus = status;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null) return NotFound();
        return View(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Reply(int ticketId, string message)
    {
        var userId = _userManager.GetUserId(User)!;
        await _ticketService.AddMessageAsync(ticketId, userId, message, isAdmin: true);
        TempData["Success"] = "Yanıt gönderildi";
        return RedirectToAction(nameof(Detail), new { id = ticketId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int ticketId, TicketStatus status)
    {
        await _ticketService.UpdateStatusAsync(ticketId, status);
        TempData["Success"] = "Destek talebi durumu güncellendi";
        return RedirectToAction(nameof(Detail), new { id = ticketId });
    }
}
