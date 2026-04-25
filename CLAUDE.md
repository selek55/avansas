# Avansas — Claude Code Context

avansas.com benzeri B2B/B2C e-ticaret platformu. .NET 8 + ASP.NET Core MVC + EF Core + Identity.

## Solution Structure

```
src/
  Avansas.Domain/         # Entities, Interfaces, Enums
  Avansas.Application/    # DTOs, Service interfaces + implementations
  Avansas.Infrastructure/ # EF Core DbContext, Repositories, DataSeeder, Migrations
  Avansas.Web/            # ASP.NET Core MVC (storefront + /admin area)
```

## Key Entities

Product, Category, Brand, ProductImage, ApplicationUser (IdentityUser), Address, Cart, CartItem, Order, OrderItem, Review, Coupon, Banner, WishlistItem, SiteSetting, StockNotification, ProductQuestion, BlogCategory, BlogPost, SupportTicket, TicketMessage, LoyaltyTransaction, ProductVariant, ReturnRequest, ReturnItem, UserNotification, PriceRule, Warehouse, WarehouseStock

## Routing

| URL | Controller/Action |
|-----|-------------------|
| `/urun/{slug}` | Product/Detail |
| `/kategori/{slug}` | Product/Category |
| `/urunler` | Product/Index |
| `/sepet` | Cart/Index |
| `/odeme` | Checkout/Index |
| `/hesap/giris\|kayit\|profil\|siparisler\|adresler` | Account |
| `/blog`, `/blog/{slug}` | Blog |
| `/destek`, `/destek/olustur`, `/destek/{id}` | Support |
| `/karsilastir` | Compare |
| `/istek-listesi` | Wishlist |
| `/toplu-siparis` | BulkOrder (B2B) |
| `/bildirimler` | Notification |
| `/sitemap.xml`, `/robots.txt` | Seo |
| `/api/address/cities\|districts` | AddressApi |
| `/chathub` | SignalR ChatHub |
| `/admin/...` | Admin Area |

## Database

- SQL Server LocalDB — `AvansasDb`
- ConnectionString key: `DefaultConnection` (`appsettings.json`)
- Migrations: `InitialCreate` + `AddNewFeatures` (9 entity) + `AddPhase2Features` (6 entity)
- `dotnet run` ile uygulama başlayınca DB otomatik migrate + seed edilir

## Seed Data

- **Admin:** admin@avansas.com / Admin123!
- **Roller:** Admin, Customer
- 6 kök kategori + 5 alt kategori, 6 marka, 6 ürün
- **Kuponlar:** `HOSGELDIN10` (%10 indirim), `KARGO0` (ücretsiz kargo)
- 2 banner

## AppSettings

```json
"AppSettings": {
  "FreeShippingThreshold": 500,
  "ShippingCost": 29.90
}
```

## Architecture Notes

- **Repository Pattern + Unit of Work** — `IUnitOfWork` → 22 `IRepository<T>` property
- **Soft delete** — `BaseEntity.IsDeleted` + EF Core `HasQueryFilter(!IsDeleted)` tüm entity'lerde
- **Cache Decorator** — `CachedProductService`, `CachedCategoryService` (IMemoryCache, 5-10dk TTL)
- **Slug** — Turkish char dönüşümü: ş→s, ğ→g, ü→u, ö→o, ç→c, ı→i
- **Misafir sepeti** — Session tabanlı, login sonrası `MergeGuestCartAsync` ile birleşir
- **Sipariş no formatı** — `AV{yyyyMMdd}{5-hane-random}`
- **Kupon validasyonu** — aktiflik → tarih → kullanım limiti → min tutar → max indirim
- **Sadakat puanı** — Siparişte 10₺ = 1 puan, yorum = 10 bonus puan
- **Google OAuth2** — Koşullu registration (placeholder credentials skip edilir)
- **Fire-and-forget email** — Sipariş/stok bildirimi `Task.Run()` ile

## Decimal Column Types (EF Core)

| Property | Type |
|----------|------|
| Price, DiscountedPrice, CostPrice | decimal(18,2) |
| TaxRate | decimal(5,2) |
| Weight | decimal(10,3) |
| SubTotal, Total, ShippingCost, TaxAmount, DiscountAmount | decimal(18,2) |

## Admin Panel

- Area: `Areas/Admin`
- Layout: `_AdminLayout.cshtml` (dark sidebar `#1a1a2e`)
- Controllers: Dashboard (Chart.js grafikleri), Products (varyant yönetimi), Orders (Excel export, fatura yazdır), Categories, Users, Brands, Coupons, Banners, Reviews, Questions (Soru-Cevap), Blog, Tickets (Destek), Settings (E-posta ayarları)
- Tüm controller action'ları için eşleşen view'lar mevcut

### Admin Views

| View | Model |
| ------ | ------- |
| Products/Create | CreateProductDto |
| Products/Edit | UpdateProductDto |
| Categories/Index | IEnumerable\<CategoryDto\> |
| Categories/Create | CreateCategoryDto |
| Categories/Edit | UpdateCategoryDto |
| Orders/Detail | OrderDto |
| Users/Index | PagedResult\<UserDto\> |
| Users/Detail | UserDto |

## Frontend Views

| View | Model |
| ------ | ------- |
| Product/Category | PagedResult\<ProductListDto\> |
| Checkout/Index | CheckoutViewModel |
| Checkout/Confirmation | OrderDto |
| Account/Profile | UserDto |
| Account/Orders | PagedResult\<OrderDto\> |
| Account/OrderDetail | OrderDto |
| Account/Addresses | IEnumerable\<AddressDto\> |

## Slug Auto-Generation (Admin Forms)

Tüm admin ürün/kategori form'larında:

- `id="nameInput"` — Ad alanı
- `id="slugInput"` — Slug alanı (Name'den otomatik dolar)
- `id="slugPreview"` — Anlık önizleme linki (`/urun/slug-adi` veya `/kategori/slug-adi`)
- Edit form'larında mevcut slug değeriyle başlatılır, `manual=true` olur
- JS `toSlug()` fonksiyonu: küçük harf + TR karakter dönüşümü + boşluk→tire + özel char temizle

## Auth / Cookie Ayarları (Program.cs)

```csharp
options.LoginPath = "/hesap/login";
options.LogoutPath = "/hesap/logout";
options.AccessDeniedPath = "/hesap/accessdenied";
```

## Sepet URL'leri (JS tarafı)

```javascript
fetch('/sepet/add', ...)          // ürün ekle
fetch('/sepet/update', ...)       // adet güncelle
fetch('/sepet/remove?cartItemId=X', {method:'POST'})  // sil
fetch('/sepet/applycoupon?couponCode=X', {method:'POST'})  // kupon uygula
// form action="/sepet/removecoupon"
```

## DTO Slug Alanları

`CreateProductDto` ve `CreateCategoryDto` içinde `public string? Slug { get; set; }` var.
Service'ler: dto.Slug doluysa onu kullanır, boşsa Name'den üretir.

## Bilinen Razor Kuralları

- `selected="@(condition ? "selected" : null)"` kullan — `@(condition ? "selected" : "")` RZ1031 hatası verir.
- `@{` bloğu `@foreach` içinde kullanılamaz — değişkeni doğrudan satırda tanımla.

## Çalıştırma

```bash
# Bağımlılıkları yükle
dotnet restore

# Uygulamayı başlat (migrate + seed otomatik)
dotnet run --project src/Avansas.Web

# Migration eklemek için
dotnet ef migrations add <MigrationName> --project src/Avansas.Infrastructure --startup-project src/Avansas.Web
```

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- For cross-module "how does X relate to Y" questions, prefer `graphify query "<question>"`, `graphify path "<A>" "<B>"`, or `graphify explain "<concept>"` over grep — these traverse the graph's EXTRACTED + INFERRED edges instead of scanning files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)
