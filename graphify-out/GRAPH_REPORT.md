# Graph Report - .  (2026-04-24)

## Corpus Check
- Corpus is ~19,054 words - fits in a single context window. You may not need a graph.

## Summary
- 394 nodes · 575 edges · 33 communities detected
- Extraction: 70% EXTRACTED · 30% INFERRED · 0% AMBIGUOUS · INFERRED: 173 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Product Catalog & Admin|Product Catalog & Admin]]
- [[_COMMUNITY_Project Architecture Docs|Project Architecture Docs]]
- [[_COMMUNITY_Order & Checkout Flow|Order & Checkout Flow]]
- [[_COMMUNITY_Cart Management|Cart Management]]
- [[_COMMUNITY_User Account & Auth|User Account & Auth]]
- [[_COMMUNITY_Category & Repository|Category & Repository]]
- [[_COMMUNITY_Domain Entities|Domain Entities]]
- [[_COMMUNITY_Product Service Interface|Product Service Interface]]
- [[_COMMUNITY_Repository Interface|Repository Interface]]
- [[_COMMUNITY_Cart Service Interface|Cart Service Interface]]
- [[_COMMUNITY_Order Service Interface|Order Service Interface]]
- [[_COMMUNITY_jQuery Validation|jQuery Validation]]
- [[_COMMUNITY_Category Service Interface|Category Service Interface]]
- [[_COMMUNITY_User Service Interface|User Service Interface]]
- [[_COMMUNITY_Data Seeder|Data Seeder]]
- [[_COMMUNITY_jQuery Validation Minified|jQuery Validation Minified]]
- [[_COMMUNITY_EF Migration|EF Migration]]
- [[_COMMUNITY_Cart DTOs|Cart DTOs]]
- [[_COMMUNITY_Order DTOs|Order DTOs]]
- [[_COMMUNITY_Product DTOs|Product DTOs]]
- [[_COMMUNITY_User DTOs|User DTOs]]
- [[_COMMUNITY_DB Model Snapshot|DB Model Snapshot]]
- [[_COMMUNITY_Category DTOs|Category DTOs]]
- [[_COMMUNITY_Unit of Work Interface|Unit of Work Interface]]
- [[_COMMUNITY_Database Context|Database Context]]
- [[_COMMUNITY_Migration Designer|Migration Designer]]
- [[_COMMUNITY_Unit of Work Impl|Unit of Work Impl]]
- [[_COMMUNITY_Pagination DTOs|Pagination DTOs]]
- [[_COMMUNITY_Identity User|Identity User]]
- [[_COMMUNITY_Module 29|Module 29]]
- [[_COMMUNITY_Module 30|Module 30]]
- [[_COMMUNITY_Module 31|Module 31]]
- [[_COMMUNITY_Module 47|Module 47]]

## God Nodes (most connected - your core abstractions)
1. `ProductService` - 17 edges
2. `OrderService` - 16 edges
3. `CartService` - 15 edges
4. `Avansas.Domain Project` - 15 edges
5. `IProductService` - 13 edges
6. `Repository` - 13 edges
7. `CategoryService` - 12 edges
8. `IRepository` - 12 edges
9. `ICartService` - 10 edges
10. `IOrderService` - 10 edges

## Surprising Connections (you probably didn't know these)
- `Avansas.Domain Build Outputs` --references--> `Avansas.Domain Project`  [EXTRACTED]
  src/Avansas.Domain/obj/Debug/net8.0/Avansas.Domain.csproj.FileListAbsolute.txt → CLAUDE.md
- `Avansas.Application Build Outputs` --references--> `Avansas.Application Project`  [EXTRACTED]
  src/Avansas.Application/obj/Debug/net8.0/Avansas.Application.csproj.FileListAbsolute.txt → CLAUDE.md
- `Avansas.Infrastructure Build Outputs` --references--> `Avansas.Infrastructure Project`  [EXTRACTED]
  src/Avansas.Infrastructure/obj/Debug/net8.0/Avansas.Infrastructure.csproj.FileListAbsolute.txt → CLAUDE.md
- `Avansas.Web Build Outputs` --references--> `Avansas.Web Project`  [EXTRACTED]
  src/Avansas.Web/obj/Debug/net8.0/Avansas.Web.csproj.FileListAbsolute.txt → CLAUDE.md

## Hyperedges (group relationships)
- **Clean Architecture Layer Dependency Chain** — claude_md_domain_project, claude_md_application_project, claude_md_infrastructure_project, claude_md_web_project [EXTRACTED 0.95]
- **Guest Cart Session Merge Flow** — claude_md_entity_cart, claude_md_guest_cart, claude_md_cart_js_api [INFERRED 0.80]
- **Slug Generation: DTO + JS toSlug() + Admin Form** — claude_md_slug_generation, claude_md_dto_createproduct, claude_md_dto_createcategory [EXTRACTED 0.90]

## Communities

### Community 0 - "Product Catalog & Admin"
Cohesion: 0.08
Nodes (5): IProductService, Product, ProductController, ProductsController, ProductService

### Community 1 - "Project Architecture Docs"
Cohesion: 0.07
Nodes (36): Admin Area (Areas/Admin), Avansas.Application Project, AppSettings (FreeShippingThreshold, ShippingCost), ASP.NET Core Identity, Avansas E-Commerce Platform, Cart JS Fetch API Endpoints, Coupon Validation Pipeline, Avansas.Domain Project (+28 more)

### Community 2 - "Order & Checkout Flow"
Cohesion: 0.09
Nodes (7): CheckoutController, Controller, DashboardController, HomeController, IOrderService, OrdersController, OrderService

### Community 3 - "Cart Management"
Cohesion: 0.15
Nodes (3): CartController, CartService, ICartService

### Community 4 - "User Account & Auth"
Cohesion: 0.1
Nodes (4): AccountController, IUserService, UsersController, UserService

### Community 5 - "Category & Repository"
Cohesion: 0.1
Nodes (5): CategoriesController, CategoryService, ICategoryService, IRepository, Repository

### Community 6 - "Domain Entities"
Cohesion: 0.08
Nodes (13): Address, Banner, BaseEntity, Brand, Cart, CartItem, Category, Coupon (+5 more)

### Community 7 - "Product Service Interface"
Cohesion: 0.14
Nodes (1): IProductService

### Community 8 - "Repository Interface"
Cohesion: 0.15
Nodes (1): IRepository

### Community 9 - "Cart Service Interface"
Cohesion: 0.18
Nodes (1): ICartService

### Community 10 - "Order Service Interface"
Cohesion: 0.18
Nodes (1): IOrderService

### Community 11 - "jQuery Validation"
Cohesion: 0.2
Nodes (2): escapeAttributeValue(), onError()

### Community 12 - "Category Service Interface"
Cohesion: 0.2
Nodes (1): ICategoryService

### Community 13 - "User Service Interface"
Cohesion: 0.2
Nodes (1): IUserService

### Community 14 - "Data Seeder"
Cohesion: 0.36
Nodes (1): DataSeeder

### Community 15 - "jQuery Validation Minified"
Cohesion: 0.33
Nodes (2): p(), u()

### Community 16 - "EF Migration"
Cohesion: 0.33
Nodes (3): Avansas.Infrastructure.Migrations, InitialCreate, Migration

### Community 17 - "Cart DTOs"
Cohesion: 0.4
Nodes (4): AddToCartDto, CartDto, CartItemDto, UpdateCartItemDto

### Community 18 - "Order DTOs"
Cohesion: 0.4
Nodes (4): CreateOrderDto, OrderDto, OrderItemDto, UpdateOrderStatusDto

### Community 19 - "Product DTOs"
Cohesion: 0.5
Nodes (4): CreateProductDto, ProductDto, ProductListDto, UpdateProductDto

### Community 20 - "User DTOs"
Cohesion: 0.4
Nodes (4): AddressDto, LoginDto, RegisterDto, UserDto

### Community 21 - "DB Model Snapshot"
Cohesion: 0.4
Nodes (3): ApplicationDbContextModelSnapshot, Avansas.Infrastructure.Migrations, ModelSnapshot

### Community 22 - "Category DTOs"
Cohesion: 0.67
Nodes (3): CategoryDto, CreateCategoryDto, UpdateCategoryDto

### Community 23 - "Unit of Work Interface"
Cohesion: 0.5
Nodes (2): IDisposable, IUnitOfWork

### Community 24 - "Database Context"
Cohesion: 0.5
Nodes (2): ApplicationDbContext, IdentityDbContext

### Community 25 - "Migration Designer"
Cohesion: 0.5
Nodes (2): Avansas.Infrastructure.Migrations, InitialCreate

### Community 26 - "Unit of Work Impl"
Cohesion: 0.5
Nodes (2): IUnitOfWork, UnitOfWork

### Community 27 - "Pagination DTOs"
Cohesion: 0.67
Nodes (2): PagedResult, ProductFilterDto

### Community 28 - "Identity User"
Cohesion: 0.67
Nodes (2): ApplicationUser, IdentityUser

### Community 29 - "Module 29"
Cohesion: 1.0
Nodes (1): Class1

### Community 30 - "Module 30"
Cohesion: 1.0
Nodes (1): BaseEntity

### Community 31 - "Module 31"
Cohesion: 1.0
Nodes (1): ErrorViewModel

### Community 47 - "Module 47"
Cohesion: 1.0
Nodes (1): Known Razor Rules (RZ1031, foreach constraints)

## Knowledge Gaps
- **40 isolated node(s):** `CartDto`, `CartItemDto`, `AddToCartDto`, `UpdateCartItemDto`, `CategoryDto` (+35 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Product Service Interface`** (14 nodes): `IProductService.cs`, `IProductService`, `.CreateProductAsync()`, `.DeleteProductAsync()`, `.GetFeaturedProductsAsync()`, `.GetNewProductsAsync()`, `.GetProductByIdAsync()`, `.GetProductBySlugAsync()`, `.GetProductsAsync()`, `.GetProductsByCategoryAsync()`, `.GetRelatedProductsAsync()`, `.IsSlugUniqueAsync()`, `.UpdateProductAsync()`, `.UpdateStockAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Repository Interface`** (13 nodes): `IRepository.cs`, `IRepository`, `.AddAsync()`, `.AddRangeAsync()`, `.CountAsync()`, `.FindAsync()`, `.FirstOrDefaultAsync()`, `.GetAllAsync()`, `.GetByIdAsync()`, `.Query()`, `.Remove()`, `.SoftDelete()`, `.Update()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Cart Service Interface`** (11 nodes): `ICartService.cs`, `ICartService`, `.AddToCartAsync()`, `.ApplyCouponAsync()`, `.ClearCartAsync()`, `.GetCartAsync()`, `.GetCartItemCountAsync()`, `.MergeGuestCartAsync()`, `.RemoveCouponAsync()`, `.RemoveFromCartAsync()`, `.UpdateCartItemAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Order Service Interface`** (11 nodes): `IOrderService.cs`, `IOrderService`, `.CalculateShippingCostAsync()`, `.CancelOrderAsync()`, `.CreateOrderFromCartAsync()`, `.GetOrderByIdAsync()`, `.GetOrderByNumberAsync()`, `.GetOrdersAsync()`, `.GetOrderStatisticsAsync()`, `.GetUserOrdersAsync()`, `.UpdateOrderStatusAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `jQuery Validation`** (11 nodes): `jquery.validate.unobtrusive.js`, `appendModelPrefix()`, `escapeAttributeValue()`, `getModelPrefix()`, `onError()`, `onErrors()`, `onReset()`, `onSuccess()`, `setValidationValues()`, `splitAndTrim()`, `validationInfo()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Category Service Interface`** (10 nodes): `ICategoryService.cs`, `ICategoryService`, `.CreateCategoryAsync()`, `.DeleteCategoryAsync()`, `.GetAllCategoriesAsync()`, `.GetCategoryByIdAsync()`, `.GetCategoryBySlugAsync()`, `.GetRootCategoriesAsync()`, `.GetSubCategoriesAsync()`, `.UpdateCategoryAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `User Service Interface`** (10 nodes): `IUserService.cs`, `IUserService`, `.AddAddressAsync()`, `.DeleteAddressAsync()`, `.GetUserAddressesAsync()`, `.GetUserByIdAsync()`, `.GetUsersAsync()`, `.SetDefaultAddressAsync()`, `.SetUserActiveStatusAsync()`, `.UpdateAddressAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Data Seeder`** (10 nodes): `DataSeeder.cs`, `DataSeeder`, `.SeedAdminUserAsync()`, `.SeedAsync()`, `.SeedBannersAsync()`, `.SeedBrandsAsync()`, `.SeedCategoriesAsync()`, `.SeedCouponsAsync()`, `.SeedProductsAsync()`, `.SeedRolesAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `jQuery Validation Minified`** (7 nodes): `jquery.validate.unobtrusive.min.js`, `f()`, `l()`, `m()`, `n()`, `p()`, `u()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Unit of Work Interface`** (4 nodes): `IUnitOfWork.cs`, `IDisposable`, `IUnitOfWork`, `.SaveChangesAsync()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Database Context`** (4 nodes): `ApplicationDbContext`, `.OnModelCreating()`, `ApplicationDbContext.cs`, `IdentityDbContext`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Migration Designer`** (4 nodes): `Avansas.Infrastructure.Migrations`, `InitialCreate`, `.BuildTargetModel()`, `20260424190550_InitialCreate.Designer.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Unit of Work Impl`** (4 nodes): `UnitOfWork.cs`, `IUnitOfWork`, `UnitOfWork`, `.Dispose()`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Pagination DTOs`** (3 nodes): `PagedResult.cs`, `PagedResult`, `ProductFilterDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Identity User`** (3 nodes): `ApplicationUser`, `ApplicationUser.cs`, `IdentityUser`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Module 29`** (2 nodes): `Class1.cs`, `Class1`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Module 30`** (2 nodes): `BaseEntity.cs`, `BaseEntity`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Module 31`** (2 nodes): `ErrorViewModel.cs`, `ErrorViewModel`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Module 47`** (1 nodes): `Known Razor Rules (RZ1031, foreach constraints)`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Product` connect `Product Catalog & Admin` to `Domain Entities`?**
  _High betweenness centrality (0.057) - this node is a cross-community bridge._
- **Why does `ProductService` connect `Product Catalog & Admin` to `Cart Management`?**
  _High betweenness centrality (0.020) - this node is a cross-community bridge._
- **Why does `Repository` connect `Category & Repository` to `Order & Checkout Flow`, `Cart Management`, `User Account & Auth`?**
  _High betweenness centrality (0.017) - this node is a cross-community bridge._
- **What connects `CartDto`, `CartItemDto`, `AddToCartDto` to the rest of the system?**
  _40 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Product Catalog & Admin` be split into smaller, more focused modules?**
  _Cohesion score 0.08 - nodes in this community are weakly interconnected._
- **Should `Project Architecture Docs` be split into smaller, more focused modules?**
  _Cohesion score 0.07 - nodes in this community are weakly interconnected._
- **Should `Order & Checkout Flow` be split into smaller, more focused modules?**
  _Cohesion score 0.09 - nodes in this community are weakly interconnected._