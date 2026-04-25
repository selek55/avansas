using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

[Authorize]
[Route("destek")]
public class SupportController : Controller
{
    private readonly ITicketService _ticketService;

    public SupportController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tickets = await _ticketService.GetUserTicketsAsync(UserId);
        return View(tickets);
    }

    [HttpGet("olustur")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var ticketId = await _ticketService.CreateTicketAsync(UserId, dto);
        TempData["Success"] = "Destek talebiniz oluşturuldu.";
        return RedirectToAction(nameof(Detail), new { id = ticketId });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null || ticket.UserId != UserId) return NotFound();

        return View(ticket);
    }

    [HttpPost("{id:int}/yanit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string message)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null || ticket.UserId != UserId) return NotFound();

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Mesaj boş olamaz.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        await _ticketService.AddMessageAsync(id, UserId, message, isAdmin: false);
        TempData["Success"] = "Mesajınız gönderildi.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
