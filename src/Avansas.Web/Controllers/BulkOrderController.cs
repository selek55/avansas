using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Controllers;

public class BulkOrderController : Controller
{
    private readonly IEmailService _emailService;

    public BulkOrderController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public IActionResult Index()
    {
        ViewData["MetaTitle"] = "Toplu Sipariş - Avansas";
        ViewData["MetaDescription"] = "Kurumsal toplu sipariş talepleriniz için bize ulaşın. Avansas'ta özel fiyat ve hızlı teslimat avantajlarından yararlanın.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string companyName, string contactName, string email, string phone, string message)
    {
        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(contactName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Lütfen zorunlu alanları doldurun.";
            return RedirectToAction(nameof(Index));
        }

        await _emailService.SendBulkOrderRequestAsync(companyName, contactName, email, phone ?? "", message);
        TempData["Success"] = "Toplu sipariş talebiniz başarıyla iletildi. En kısa sürede sizinle iletişime geçeceğiz.";
        return RedirectToAction(nameof(Index));
    }
}
