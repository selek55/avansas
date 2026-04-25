using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QRCoder;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private readonly IUserService _userService;
    private readonly ICartService _cartService;
    private readonly IEmailService _emailService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IReturnService _returnService;
    private readonly IInvoiceService _invoiceService;
    private readonly IShipmentService _shipmentService;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        IOrderService orderService, IUserService userService, ICartService cartService, IEmailService emailService,
        ILoyaltyService loyaltyService, IReturnService returnService,
        IInvoiceService invoiceService, IShipmentService shipmentService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _orderService = orderService;
        _userService = userService;
        _cartService = cartService;
        _emailService = emailService;
        _loyaltyService = loyaltyService;
        _returnService = returnService;
        _invoiceService = invoiceService;
        _shipmentService = shipmentService;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, dto.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                var sessionId = HttpContext.Session.Id;
                await _cartService.MergeGuestCartAsync(sessionId, user.Id);
            }
            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.RequiresTwoFactor)
            return RedirectToAction(nameof(Verify2FA), new { returnUrl });

        ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre");
        return View(dto);
    }

    public IActionResult Register() => View();

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (dto.Password != dto.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Şifreler eşleşmiyor");
            return View(dto);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email, Email = dto.Email,
            FirstName = dto.FirstName, LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber, IsCorporate = dto.IsCorporate,
            CompanyName = dto.CompanyName, TaxNumber = dto.TaxNumber, TaxOffice = dto.TaxOffice
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userService.GetUserByIdAsync(userId);
        var loyalty = await _loyaltyService.GetUserSummaryAsync(userId);
        ViewBag.Loyalty = loyalty;
        return View(user);
    }

    [Authorize]
    public async Task<IActionResult> Orders(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await _orderService.GetUserOrdersAsync(userId, page);
        return View(orders);
    }

    [Authorize]
    public async Task<IActionResult> OrderDetail(string orderNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (order == null || order.UserId != userId) return NotFound();
        return View(order);
    }

    [Authorize]
    public async Task<IActionResult> Addresses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var addresses = await _userService.GetUserAddressesAsync(userId);
        return View(addresses);
    }

    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "E-posta adresi gereklidir");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token, email }, Request.Scheme)!;
            var fullName = $"{user.FirstName} {user.LastName}";
            _ = Task.Run(() => _emailService.SendPasswordResetAsync(email, fullName, resetLink));
        }

        // Kullanıcı bulunamasa da aynı mesajı göster (güvenlik)
        TempData["Success"] = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    public IActionResult ResetPassword(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));
        ViewBag.Token = token;
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string token, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Şifreler eşleşmiyor");
            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            TempData["Success"] = "Şifreniz başarıyla sıfırlandı.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (result.Succeeded)
        {
            TempData["Success"] = "Şifreniz başarıyla sıfırlandı. Giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        ViewBag.Token = token;
        ViewBag.Email = email;
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Returns()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var returns = await _returnService.GetUserReturnsAsync(userId);
        return View(returns);
    }

    [Authorize]
    public async Task<IActionResult> CreateReturn(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId) return NotFound();
        return View(order);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReturn(CreateReturnRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Lütfen gerekli alanları doldurun.";
            return RedirectToAction(nameof(Returns));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            await _returnService.CreateReturnAsync(userId, dto);
            TempData["Success"] = "İade talebiniz başarıyla oluşturuldu.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Returns));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelReturn(int id)
    {
        try
        {
            await _returnService.CancelReturnAsync(id);
            TempData["Success"] = "İade talebiniz iptal edildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Returns));
    }

    public IActionResult AccessDenied() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            TempData["Error"] = $"Harici giriş hatası: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["Error"] = "Harici giriş bilgisi alınamadı.";
            return RedirectToAction(nameof(Login));
        }

        // Try to sign in with existing external login
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);
        if (result.Succeeded)
        {
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null)
            {
                var sessionId = HttpContext.Session.Id;
                await _cartService.MergeGuestCartAsync(sessionId, existingUser.Id);
            }
            return LocalRedirect(returnUrl ?? "/");
        }

        // Create new user from external login
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            TempData["Error"] = "Google hesabınızdan e-posta bilgisi alınamadı.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                TempData["Error"] = "Hesap oluşturulurken bir hata oluştu.";
                return RedirectToAction(nameof(Login));
            }
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: true);

        var sid = HttpContext.Session.Id;
        await _cartService.MergeGuestCartAsync(sid, user.Id);

        return LocalRedirect(returnUrl ?? "/");
    }

    // ── Phase 3: PDF Fatura ──────────────────────────────────────────────────
    [Authorize]
    public async Task<IActionResult> DownloadInvoice(string orderNumber)
    {
        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (order == null || order.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return NotFound();

        var pdfBytes = await _invoiceService.GenerateOrderInvoicePdfAsync(order.Id);
        var invoiceNo = _invoiceService.GetInvoiceNumber(order.Id);
        return File(pdfBytes, "application/pdf", $"{invoiceNo}.pdf");
    }

    // ── Phase 4: Kargo Takip ─────────────────────────────────────────────────
    public async Task<IActionResult> TrackShipment(string trackingNumber)
    {
        var tracking = await _shipmentService.GetTrackingByNumberAsync(trackingNumber);
        return View(tracking);
    }

    // ── Phase 6B: Hızlı Tekrar Sipariş ─────────────────────────────────────
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Reorder(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            await _cartService.ReorderFromOrderAsync(userId, orderId);
            TempData["Success"] = "Siparişteki ürünler sepetinize eklendi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message;
        }
        return RedirectToAction("Index", "Cart");
    }

    // ── Phase 6A: 2FA ────────────────────────────────────────────────────────
    [Authorize]
    public async Task<IActionResult> Enable2FA()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        var issuer = Uri.EscapeDataString("Avansas");
        var account = Uri.EscapeDataString(user.Email!);
        var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6";

        // QR kod base64 görüntüsü oluştur
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(20);
        ViewBag.QrCodeImage = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        ViewBag.AuthKey = key;

        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Enable2FA(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code.Replace(" ", ""));

        if (!isValid)
        {
            ModelState.AddModelError("", "Geçersiz kod. Lütfen tekrar deneyin.");
            return await Enable2FA();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        TempData["Success"] = "İki faktörlü kimlik doğrulama etkinleştirildi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Disable2FA()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        TempData["Success"] = "İki faktörlü kimlik doğrulama devre dışı bırakıldı.";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Verify2FA(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToAction(nameof(Login));
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Verify2FA(string code, bool rememberMe, string? returnUrl = null)
    {
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code.Replace(" ", ""), rememberMe, rememberMe);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError("", "Geçersiz doğrulama kodu.");
        return View();
    }
}
