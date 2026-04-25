using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedCategoriesAsync(context);
        await SeedBrandsAsync(context);
        await SeedBannersAsync(context);
        await SeedProductsAsync(context);
        await SeedCouponsAsync(context);
        await FixProductBrandsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "Customer", "Manager" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync("admin@avansas.com") != null) return;

        var admin = new ApplicationUser
        {
            UserName = "admin@avansas.com", Email = "admin@avansas.com",
            FirstName = "Admin", LastName = "Kullanıcı",
            EmailConfirmed = true, IsActive = true
        };
        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Name = "Kırtasiye", Slug = "kirtasiye", DisplayOrder = 1, IsActive = true },
            new() { Name = "Ofis Malzemeleri", Slug = "ofis-malzemeleri", DisplayOrder = 2, IsActive = true },
            new() { Name = "Temizlik Ürünleri", Slug = "temizlik-urunleri", DisplayOrder = 3, IsActive = true },
            new() { Name = "Teknoloji", Slug = "teknoloji", DisplayOrder = 4, IsActive = true },
            new() { Name = "Mobilya & Dekorasyon", Slug = "mobilya-dekorasyon", DisplayOrder = 5, IsActive = true },
            new() { Name = "Kahve & Çay", Slug = "kahve-cay", DisplayOrder = 6, IsActive = true },
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        var subCategories = new List<Category>
        {
            new() { Name = "Kalemler", Slug = "kalemler", ParentCategoryId = categories[0].Id, DisplayOrder = 1, IsActive = true },
            new() { Name = "Defterler", Slug = "defterler", ParentCategoryId = categories[0].Id, DisplayOrder = 2, IsActive = true },
            new() { Name = "Dosyalama", Slug = "dosyalama", ParentCategoryId = categories[0].Id, DisplayOrder = 3, IsActive = true },
            new() { Name = "Yazıcı & Toner", Slug = "yazici-toner", ParentCategoryId = categories[1].Id, DisplayOrder = 1, IsActive = true },
            new() { Name = "Kartuş & Mürekkep", Slug = "kartus-murekkep", ParentCategoryId = categories[1].Id, DisplayOrder = 2, IsActive = true },
        };

        context.Categories.AddRange(subCategories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBrandsAsync(ApplicationDbContext context)
    {
        if (await context.Brands.AnyAsync()) return;

        context.Brands.AddRange(
            new Brand { Name = "Pilot", Slug = "pilot", IsActive = true },
            new Brand { Name = "Faber-Castell", Slug = "faber-castell", IsActive = true },
            new Brand { Name = "Staples", Slug = "staples", IsActive = true },
            new Brand { Name = "HP", Slug = "hp", IsActive = true },
            new Brand { Name = "Canon", Slug = "canon", IsActive = true },
            new Brand { Name = "Nescafe", Slug = "nescafe", IsActive = true }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedBannersAsync(ApplicationDbContext context)
    {
        if (await context.Banners.AnyAsync()) return;

        context.Banners.AddRange(
            new Banner { Title = "Ofis İhtiyaçlarınız Tek Adreste", SubTitle = "500₺ ve üzeri alışverişlerde ücretsiz kargo", ImageUrl = "/images/banners/banner1.jpg", LinkUrl = "/urunler", ButtonText = "Hemen Alışverişe Başla", DisplayOrder = 1, IsActive = true },
            new Banner { Title = "Yeni Sezon Kırtasiye Ürünleri", SubTitle = "En yeni ürünleri keşfedin", ImageUrl = "/images/banners/banner2.jpg", LinkUrl = "/kategori/kirtasiye", ButtonText = "Keşfet", DisplayOrder = 2, IsActive = true }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var kalemlerCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "kalemler");
        var defterlerCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "defterler");
        var pilotBrand = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "pilot");
        var faberBrand = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "faber-castell");

        if (kalemlerCategory == null || pilotBrand == null) return;

        var products = new List<Product>
        {
            new() { Name = "Pilot G2 Jel Kalem Siyah 0.7mm", Slug = "pilot-g2-jel-kalem-siyah-07mm", SKU = "PIL-G2-BLK-07", Price = 45.90m, DiscountedPrice = 39.90m, StockQuantity = 250, CategoryId = kalemlerCategory.Id, BrandId = pilotBrand.Id, IsActive = true, IsFeatured = true, IsNewProduct = false, TaxRate = 18, ShortDescription = "Smooth yazım deneyimi için G2 jel kalem", MainImageUrl = "/images/products/pilot-g2.jpg", Unit = "Adet", Weight = 0.05m },
            new() { Name = "Faber-Castell Tükenmez Kalem Seti 10'lu", Slug = "faber-castell-tukenmez-kalem-seti-10lu", SKU = "FC-TKM-SET-10", Price = 89.90m, StockQuantity = 180, CategoryId = kalemlerCategory.Id, BrandId = faberBrand?.Id, IsActive = true, IsFeatured = true, TaxRate = 18, ShortDescription = "10 farklı renk tükenmez kalem seti", MainImageUrl = "/images/products/faber-set.jpg", Unit = "Set", Weight = 0.15m },
            new() { Name = "A4 Kareli Defter 80 Yaprak", Slug = "a4-kareli-defter-80-yaprak", SKU = "NTB-A4-KAR-80", Price = 34.90m, DiscountedPrice = 29.90m, StockQuantity = 500, CategoryId = defterlerCategory?.Id ?? kalemlerCategory.Id, IsActive = true, IsNewProduct = true, TaxRate = 8, ShortDescription = "Kaliteli beyaz kağıt, 80 yaprak A4 kareli defter", MainImageUrl = "/images/products/defter-a4.jpg", Unit = "Adet", Weight = 0.2m },
            new() { Name = "Post-It Yapışkanlı Not Kağıdı Sarı 76x76mm", Slug = "post-it-yapiskanli-not-kagidi-sari-76x76mm", SKU = "PST-YLW-76", Price = 24.90m, StockQuantity = 800, CategoryId = kalemlerCategory.Id, IsActive = true, IsFeatured = false, TaxRate = 18, ShortDescription = "100 yaprak, yeniden yapıştırılabilir", MainImageUrl = "/images/products/post-it.jpg", Unit = "Paket", Weight = 0.1m },
            new() { Name = "Plastik Şeffaf Dosya A4 50'li Paket", Slug = "plastik-seffaf-dosya-a4-50li-paket", SKU = "DYS-A4-50", Price = 59.90m, StockQuantity = 300, CategoryId = kalemlerCategory.Id, IsActive = true, IsFeatured = true, TaxRate = 18, ShortDescription = "A4 boyutunda şeffaf plastik dosya", MainImageUrl = "/images/products/dosya.jpg", Unit = "Paket", Weight = 0.5m },
            new() { Name = "Makas 21cm Paslanmaz Çelik", Slug = "makas-21cm-paslanmaz-celik", SKU = "MKS-21CM", Price = 49.90m, DiscountedPrice = 44.90m, StockQuantity = 150, CategoryId = kalemlerCategory.Id, IsActive = true, TaxRate = 18, ShortDescription = "Ergonomik saplı, keskin makas", MainImageUrl = "/images/products/makas.jpg", Unit = "Adet", Weight = 0.08m },
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task FixProductBrandsAsync(ApplicationDbContext context)
    {
        var staplesBrand = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "staples");
        var faberBrand = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "faber-castell");
        if (staplesBrand == null) return;

        var slugsToFix = new Dictionary<string, int?>
        {
            ["a4-kareli-defter-80-yaprak"] = faberBrand?.Id,
            ["post-it-yapiskanli-not-kagidi-sari-76x76mm"] = staplesBrand.Id,
            ["plastik-seffaf-dosya-a4-50li-paket"] = staplesBrand.Id,
            ["makas-21cm-paslanmaz-celik"] = staplesBrand.Id,
        };

        foreach (var (slug, brandId) in slugsToFix)
        {
            if (brandId == null) continue;
            var product = await context.Products.FirstOrDefaultAsync(p => p.Slug == slug && p.BrandId == null);
            if (product != null)
            {
                product.BrandId = brandId;
                context.Products.Update(product);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCouponsAsync(ApplicationDbContext context)
    {
        if (await context.Coupons.AnyAsync()) return;

        context.Coupons.AddRange(
            new Coupon { Code = "HOSGELDIN10", Description = "Hoş geldin kuponu - %10 indirim", DiscountType = DiscountType.Percentage, DiscountValue = 10, MinOrderAmount = 100, MaxDiscountAmount = 50, IsActive = true, ValidTo = DateTime.UtcNow.AddYears(1) },
            new Coupon { Code = "KARGO0", Description = "Ücretsiz kargo kuponu", DiscountType = DiscountType.FixedAmount, DiscountValue = 29.90m, MinOrderAmount = 200, IsActive = true, ValidTo = DateTime.UtcNow.AddMonths(6) }
        );
        await context.SaveChangesAsync();
    }
}
