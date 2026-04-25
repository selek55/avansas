using Avansas.Application.Interfaces;
using Avansas.Application.Services;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Avansas.Infrastructure.Data;
using Avansas.Infrastructure.Repositories;
using Avansas.Infrastructure.Services;
using Avansas.Web.Filters;
using Avansas.Web.Hubs;
using Avansas.Web.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/hesap/giris";
    options.LogoutPath = "/hesap/cikis";
    options.AccessDeniedPath = "/hesap/accessdenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/admin"))
        {
            var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
            ctx.Response.Redirect($"/admin/giris?returnUrl={returnUrl}");
        }
        else
        {
            ctx.Response.Redirect(ctx.RedirectUri);
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/admin"))
            ctx.Response.Redirect("/admin/giris");
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// Google External Login
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && googleClientId != "YOUR_GOOGLE_CLIENT_ID")
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret!;
        });
}

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Repositories & Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// File Service
builder.Services.AddScoped<IFileService, LocalFileService>();

// Email Service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Application Services (with cache decorators)
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductService>(sp =>
    new CachedProductService(sp.GetRequiredService<ProductService>(), sp.GetRequiredService<IMemoryCache>()));
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ICategoryService>(sp =>
    new CachedCategoryService(sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<IMemoryCache>()));
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<ISiteSettingService, SiteSettingService>();
builder.Services.AddScoped<IProductQuestionService, ProductQuestionService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IStockNotificationService, StockNotificationService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPriceRuleService, PriceRuleService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// Phase 2: iyzico Ödeme
builder.Services.AddScoped<IPaymentService, IyzicoPaymentService>();

// Phase 3: PDF Fatura
builder.Services.AddScoped<IInvoiceService, PdfInvoiceService>();

// Phase 4: Kargo Takip
builder.Services.AddScoped<IShipmentService, ShipmentService>();

// Phase 5: Ürün Önerileri (cache decorator ile)
builder.Services.AddScoped<RecommendationService>();
builder.Services.AddScoped<IRecommendationService>(sp =>
    new CachedRecommendationService(sp.GetRequiredService<RecommendationService>(), sp.GetRequiredService<IMemoryCache>()));

// Phase 6: Hediye Kartı
builder.Services.AddScoped<IGiftCardService, GiftCardService>();

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer();

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// Localization (i18n)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Çok fazla istek gönderdiniz. Lütfen biraz bekleyin.", cancellationToken);
    };
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DataSeeder.SeedAsync(context, userManager, roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/hata");
    app.UseStatusCodePagesWithReExecute("/hata/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Request Localization (i18n)
var supportedCultures = new[] { new CultureInfo("tr-TR"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider()
    }
});

app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Admin login route (unauthenticated, before area route)
app.MapControllerRoute(
    name: "admin-login",
    pattern: "admin/giris",
    defaults: new { area = "Admin", controller = "Account", action = "Login" });

// Admin area route
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" },
    constraints: new { },
    dataTokens: new { area = "Admin" });

// Turkish-friendly routes
app.MapControllerRoute("product-detail", "urun/{slug}", new { controller = "Product", action = "Detail" });
app.MapControllerRoute("stock-notify", "urun/stock-notify", new { controller = "Product", action = "StockNotify" });
app.MapControllerRoute("ask-question", "urun/ask-question", new { controller = "Product", action = "AskQuestion" });
app.MapControllerRoute("category", "kategori/{slug}", new { controller = "Product", action = "Category" });
app.MapControllerRoute("product-suggestions", "urunler/suggestions", new { controller = "Product", action = "Suggestions" });
app.MapControllerRoute("products", "urunler/{action=Index}/{id?}", new { controller = "Product" });
app.MapControllerRoute("cart", "sepet/{action=Index}/{id?}", new { controller = "Cart" });
app.MapControllerRoute("checkout", "odeme/{action=Index}/{id?}", new { controller = "Checkout" });
// Explicit Turkish account routes
app.MapControllerRoute("account-login-tr", "hesap/giris", new { controller = "Account", action = "Login" });
app.MapControllerRoute("account-register-tr", "hesap/kayit", new { controller = "Account", action = "Register" });
app.MapControllerRoute("account-logout-tr", "hesap/cikis", new { controller = "Account", action = "Logout" });
app.MapControllerRoute("account-profile-tr", "hesap/profil", new { controller = "Account", action = "Profile" });
app.MapControllerRoute("account-orders-tr", "hesap/siparisler", new { controller = "Account", action = "Orders" });
app.MapControllerRoute("account-order-detail-tr", "hesap/siparis/{orderNumber}", new { controller = "Account", action = "OrderDetail" });
app.MapControllerRoute("account-addresses-tr", "hesap/adresler", new { controller = "Account", action = "Addresses" });
app.MapControllerRoute("account-returns-tr", "hesap/iadeler", new { controller = "Account", action = "Returns" });
app.MapControllerRoute("account-create-return-tr", "hesap/iade-olustur", new { controller = "Account", action = "CreateReturn" });
app.MapControllerRoute("account-cancel-return-tr", "hesap/iade-iptal/{id:int}", new { controller = "Account", action = "CancelReturn" });
app.MapControllerRoute("forgot-password", "hesap/sifremi-unuttum", new { controller = "Account", action = "ForgotPassword" });
app.MapControllerRoute("reset-password", "hesap/sifre-sifirla", new { controller = "Account", action = "ResetPassword" });
app.MapControllerRoute("account", "hesap/{action=Login}/{id?}", new { controller = "Account" });
app.MapControllerRoute("review-create", "yorum/olustur", new { controller = "Review", action = "Create" });
app.MapControllerRoute("wishlist", "istek-listesi/{action=Index}/{id?}", new { controller = "Wishlist" });
app.MapControllerRoute("blog-detail", "blog/{slug}", new { controller = "Blog", action = "Detail" });
app.MapControllerRoute("blog", "blog", new { controller = "Blog", action = "Index" });
app.MapControllerRoute("destek-olustur", "destek/olustur", new { controller = "Support", action = "Create" });
app.MapControllerRoute("destek-detay", "destek/{id:int}", new { controller = "Support", action = "Detail" });
app.MapControllerRoute("destek", "destek/{action=Index}", new { controller = "Support" });
app.MapControllerRoute("karsilastir", "karsilastir/{action=Index}", new { controller = "Compare" });
app.MapControllerRoute("bildirimler", "bildirimler/{action=Index}/{id?}", new { controller = "Notification" });
app.MapControllerRoute("toplu-siparis-submit", "toplu-siparis/submit", new { controller = "BulkOrder", action = "Submit" });
app.MapControllerRoute("toplu-siparis", "toplu-siparis", new { controller = "BulkOrder", action = "Index" });

// Phase 2: Ödeme routes
app.MapControllerRoute("3d-callback", "odeme/3d-dogrulama", new { controller = "Checkout", action = "ThreeDSecureCallback" });
app.MapControllerRoute("installment-check", "odeme/taksit-sorgula", new { controller = "Checkout", action = "CheckInstallment" });

// Phase 3: Fatura
app.MapControllerRoute("download-invoice", "hesap/fatura/{orderNumber}", new { controller = "Account", action = "DownloadInvoice" });

// Phase 4: Kargo Takip
app.MapControllerRoute("cargo-tracking", "kargo-takip/{trackingNumber}", new { controller = "Account", action = "TrackShipment" });

// Phase 6B: Tekrar Sipariş
app.MapControllerRoute("reorder", "hesap/tekrar-siparis/{orderId:int}", new { controller = "Account", action = "Reorder" });

// Phase 6A: 2FA
app.MapControllerRoute("enable-2fa", "hesap/2fa-etkinlestir", new { controller = "Account", action = "Enable2FA" });
app.MapControllerRoute("verify-2fa", "hesap/2fa-dogrula", new { controller = "Account", action = "Verify2FA" });
app.MapControllerRoute("disable-2fa", "hesap/2fa-devre-disi", new { controller = "Account", action = "Disable2FA" });

// Phase 6E: Hediye Kartı
app.MapControllerRoute("gift-card", "hediye-karti/{action=Index}/{id?}", new { controller = "GiftCard" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chathub");

// Hangfire Dashboard (/admin/jobs — sadece Admin rolü erişebilir)
app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() },
    DashboardTitle = "Avansas — Arka Plan Görevleri"
});

// Tekrarlayan görevler
RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "abandoned-cart-emails",
    x => x.SendAbandonedCartEmailsAsync(),
    "0 */2 * * *");   // Her 2 saatte bir

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "stock-notifications",
    x => x.ProcessStockNotificationsAsync(),
    "*/30 * * * *");  // Her 30 dakikada bir

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "scheduled-price-rules",
    x => x.ProcessScheduledPriceRulesAsync(),
    "*/15 * * * *");  // Her 15 dakikada bir

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "cleanup-expired",
    x => x.CleanupExpiredDataAsync(),
    "0 3 * * *");     // Her gün 03:00

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "payment-reminders",
    x => x.SendPaymentReminderEmailsAsync(),
    "0 9 * * *");     // Her gün 09:00

app.Run();
