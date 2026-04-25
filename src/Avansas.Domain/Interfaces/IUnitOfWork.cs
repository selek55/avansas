using Avansas.Domain.Entities;

namespace Avansas.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IRepository<Brand> Brands { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Cart> Carts { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<Address> Addresses { get; }
    IRepository<Review> Reviews { get; }
    IRepository<Coupon> Coupons { get; }
    IRepository<Banner> Banners { get; }
    IRepository<ProductImage> ProductImages { get; }
    IRepository<WishlistItem> WishlistItems { get; }
    IRepository<SiteSetting> SiteSettings { get; }
    IRepository<StockNotification> StockNotifications { get; }
    IRepository<ProductQuestion> ProductQuestions { get; }
    IRepository<BlogPost> BlogPosts { get; }
    IRepository<BlogCategory> BlogCategories { get; }
    IRepository<SupportTicket> SupportTickets { get; }
    IRepository<TicketMessage> TicketMessages { get; }
    IRepository<LoyaltyTransaction> LoyaltyTransactions { get; }
    IRepository<ProductVariant> ProductVariants { get; }
    IRepository<ReturnRequest> ReturnRequests { get; }
    IRepository<ReturnItem> ReturnItems { get; }
    IRepository<UserNotification> UserNotifications { get; }
    IRepository<PriceRule> PriceRules { get; }
    IRepository<Warehouse> Warehouses { get; }
    IRepository<WarehouseStock> WarehouseStocks { get; }

    // Phase 2-6 yeni entity'ler
    IRepository<PaymentTransaction> PaymentTransactions { get; }
    IRepository<ShipmentTracking> ShipmentTrackings { get; }
    IRepository<ShipmentTrackingEvent> ShipmentTrackingEvents { get; }
    IRepository<ProductView> ProductViews { get; }
    IRepository<GiftCard> GiftCards { get; }
    IRepository<GiftCardTransaction> GiftCardTransactions { get; }

    Task<int> SaveChangesAsync();
}
