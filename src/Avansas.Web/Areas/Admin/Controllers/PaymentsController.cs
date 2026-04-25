using Avansas.Application.Interfaces;
using Avansas.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PaymentsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;

    public PaymentsController(IUnitOfWork unitOfWork, IPaymentService paymentService)
    {
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var transactions = await _unitOfWork.PaymentTransactions.Query()
            .Include(t => t.Order)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * 20)
            .Take(20)
            .ToListAsync();

        ViewBag.Page = page;
        return View(transactions);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var tx = await _unitOfWork.PaymentTransactions.Query()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tx == null) return NotFound();
        return View(tx);
    }

    [HttpPost]
    public async Task<IActionResult> Refund(int orderId)
    {
        var result = await _paymentService.RefundPaymentAsync(orderId);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? $"{result.RefundedAmount:N2} ₺ iade işlemi başlatıldı."
            : $"İade başarısız: {result.ErrorMessage}";
        return RedirectToAction(nameof(Index));
    }
}
