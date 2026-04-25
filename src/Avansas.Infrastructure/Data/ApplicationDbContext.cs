using Avansas.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
    public DbSet<StockNotification> StockNotifications { get; set; }
    public DbSet<ProductQuestion> ProductQuestions { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogCategory> BlogCategories { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<TicketMessage> TicketMessages { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ReturnRequest> ReturnRequests { get; set; }
    public DbSet<ReturnItem> ReturnItems { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<PriceRule> PriceRules { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseStock> WarehouseStocks { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<ShipmentTracking> ShipmentTrackings { get; set; }
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents { get; set; }
    public DbSet<ProductView> ProductViews { get; set; }
    public DbSet<GiftCard> GiftCards { get; set; }
    public DbSet<GiftCardTransaction> GiftCardTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.DiscountedPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.TaxRate).HasColumnType("decimal(5,2)");
            e.Property(p => p.Weight).HasColumnType("decimal(10,3)");
            e.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Brand).WithMany(b => b.Products).HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProductImage>(e =>
        {
            e.HasQueryFilter(i => !i.IsDeleted);
            e.HasOne(i => i.Product).WithMany(p => p.Images).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(e =>
        {
            e.HasQueryFilter(c => !c.IsDeleted);
            e.HasOne(c => c.ParentCategory).WithMany(c => c.SubCategories).HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(e =>
        {
            e.HasQueryFilter(o => !o.IsDeleted);
            e.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
            e.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.TaxAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.Total).HasColumnType("decimal(18,2)");
            e.HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.HasQueryFilter(i => !i.IsDeleted);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.TaxRate).HasColumnType("decimal(5,2)");
            e.HasOne(i => i.Product).WithMany(p => p.OrderItems).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Cart>(e =>
        {
            e.HasQueryFilter(c => !c.IsDeleted);
            e.Property(c => c.DiscountAmount).HasColumnType("decimal(18,2)");
            e.HasOne(c => c.User).WithOne(u => u.Cart).HasForeignKey<Cart>(c => c.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CartItem>(e =>
        {
            e.HasQueryFilter(i => !i.IsDeleted);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Coupon>(e =>
        {
            e.HasQueryFilter(c => !c.IsDeleted);
            e.Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
            e.Property(c => c.MinOrderAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(c => c.Code).IsUnique();
        });

        builder.Entity<Review>(e => e.HasQueryFilter(r => !r.IsDeleted));
        builder.Entity<Banner>(e => e.HasQueryFilter(b => !b.IsDeleted));
        builder.Entity<Address>(e => e.HasQueryFilter(a => !a.IsDeleted));

        builder.Entity<WishlistItem>(e =>
        {
            e.HasQueryFilter(w => !w.IsDeleted);
            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        });

        builder.Entity<SiteSetting>(e =>
        {
            e.HasQueryFilter(s => !s.IsDeleted);
            e.HasIndex(s => s.Key).IsUnique();
        });

        builder.Entity<StockNotification>(e =>
        {
            e.HasQueryFilter(s => !s.IsDeleted);
            e.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductQuestion>(e =>
        {
            e.HasQueryFilter(q => !q.IsDeleted);
            e.HasOne(q => q.Product).WithMany().HasForeignKey(q => q.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(q => q.User).WithMany().HasForeignKey(q => q.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BlogPost>(e =>
        {
            e.HasQueryFilter(b => !b.IsDeleted);
            e.HasOne(b => b.Author).WithMany().HasForeignKey(b => b.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.BlogCategory).WithMany(c => c.Posts).HasForeignKey(b => b.BlogCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BlogCategory>(e => e.HasQueryFilter(c => !c.IsDeleted));

        builder.Entity<SupportTicket>(e =>
        {
            e.HasQueryFilter(t => !t.IsDeleted);
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Order).WithMany().HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TicketMessage>(e =>
        {
            e.HasQueryFilter(m => !m.IsDeleted);
            e.HasOne(m => m.Ticket).WithMany(t => t.Messages).HasForeignKey(m => m.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoyaltyTransaction>(e =>
        {
            e.HasQueryFilter(l => !l.IsDeleted);
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Order).WithMany().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProductVariant>(e =>
        {
            e.HasQueryFilter(v => !v.IsDeleted);
            e.Property(v => v.PriceAdjustment).HasColumnType("decimal(18,2)");
            e.HasOne(v => v.Product).WithMany().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReturnRequest>(e =>
        {
            e.HasQueryFilter(r => !r.IsDeleted);
            e.Property(r => r.RefundAmount).HasColumnType("decimal(18,2)");
            e.HasOne(r => r.Order).WithMany().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReturnItem>(e =>
        {
            e.HasQueryFilter(i => !i.IsDeleted);
            e.HasOne(i => i.ReturnRequest).WithMany(r => r.Items).HasForeignKey(i => i.ReturnRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.OrderItem).WithMany().HasForeignKey(i => i.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserNotification>(e =>
        {
            e.HasQueryFilter(n => !n.IsDeleted);
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });

        builder.Entity<PriceRule>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
            e.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Brand).WithMany().HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Warehouse>(e =>
        {
            e.HasQueryFilter(w => !w.IsDeleted);
        });

        builder.Entity<WarehouseStock>(e =>
        {
            e.HasQueryFilter(s => !s.IsDeleted);
            e.HasOne(s => s.Warehouse).WithMany(w => w.Stocks).HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.WarehouseId, s.ProductId }).IsUnique();
        });

        builder.Entity<PaymentTransaction>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Order).WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShipmentTracking>(e =>
        {
            e.HasQueryFilter(s => !s.IsDeleted);
            e.HasOne(s => s.Order).WithMany().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShipmentTrackingEvent>(e =>
        {
            e.HasQueryFilter(s => !s.IsDeleted);
            e.HasOne(s => s.ShipmentTracking).WithMany(t => t.Events).HasForeignKey(s => s.ShipmentTrackingId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductView>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
            e.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.UserId, p.ProductId });
            e.HasIndex(p => new { p.SessionId, p.ProductId });
        });

        builder.Entity<GiftCard>(e =>
        {
            e.HasQueryFilter(g => !g.IsDeleted);
            e.Property(g => g.InitialBalance).HasColumnType("decimal(18,2)");
            e.Property(g => g.RemainingBalance).HasColumnType("decimal(18,2)");
            e.HasIndex(g => g.Code).IsUnique();
            e.HasOne(g => g.Purchaser).WithMany().HasForeignKey(g => g.PurchaserUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GiftCardTransaction>(e =>
        {
            e.HasQueryFilter(g => !g.IsDeleted);
            e.Property(g => g.Amount).HasColumnType("decimal(18,2)");
            e.Property(g => g.BalanceAfter).HasColumnType("decimal(18,2)");
            e.HasOne(g => g.GiftCard).WithMany(c => c.Transactions).HasForeignKey(g => g.GiftCardId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Order).WithMany().HasForeignKey(g => g.OrderId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
