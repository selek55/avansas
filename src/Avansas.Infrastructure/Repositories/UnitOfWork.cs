using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Avansas.Infrastructure.Data;

namespace Avansas.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Products = new Repository<Product>(context);
        Categories = new Repository<Category>(context);
        Brands = new Repository<Brand>(context);
        Orders = new Repository<Order>(context);
        OrderItems = new Repository<OrderItem>(context);
        Carts = new Repository<Cart>(context);
        CartItems = new Repository<CartItem>(context);
        Addresses = new Repository<Address>(context);
        Reviews = new Repository<Review>(context);
        Coupons = new Repository<Coupon>(context);
        Banners = new Repository<Banner>(context);
        ProductImages = new Repository<ProductImage>(context);
        WishlistItems = new Repository<WishlistItem>(context);
        SiteSettings = new Repository<SiteSetting>(context);
        StockNotifications = new Repository<StockNotification>(context);
        ProductQuestions = new Repository<ProductQuestion>(context);
        BlogPosts = new Repository<BlogPost>(context);
        BlogCategories = new Repository<BlogCategory>(context);
        SupportTickets = new Repository<SupportTicket>(context);
        TicketMessages = new Repository<TicketMessage>(context);
        LoyaltyTransactions = new Repository<LoyaltyTransaction>(context);
        ProductVariants = new Repository<ProductVariant>(context);
        ReturnRequests = new Repository<ReturnRequest>(context);
        ReturnItems = new Repository<ReturnItem>(context);
        UserNotifications = new Repository<UserNotification>(context);
        PriceRules = new Repository<PriceRule>(context);
        Warehouses = new Repository<Warehouse>(context);
        WarehouseStocks = new Repository<WarehouseStock>(context);
        PaymentTransactions = new Repository<PaymentTransaction>(context);
        ShipmentTrackings = new Repository<ShipmentTracking>(context);
        ShipmentTrackingEvents = new Repository<ShipmentTrackingEvent>(context);
        ProductViews = new Repository<ProductView>(context);
        GiftCards = new Repository<GiftCard>(context);
        GiftCardTransactions = new Repository<GiftCardTransaction>(context);
    }

    public IRepository<Product> Products { get; }
    public IRepository<Category> Categories { get; }
    public IRepository<Brand> Brands { get; }
    public IRepository<Order> Orders { get; }
    public IRepository<OrderItem> OrderItems { get; }
    public IRepository<Cart> Carts { get; }
    public IRepository<CartItem> CartItems { get; }
    public IRepository<Address> Addresses { get; }
    public IRepository<Review> Reviews { get; }
    public IRepository<Coupon> Coupons { get; }
    public IRepository<Banner> Banners { get; }
    public IRepository<ProductImage> ProductImages { get; }
    public IRepository<WishlistItem> WishlistItems { get; }
    public IRepository<SiteSetting> SiteSettings { get; }
    public IRepository<StockNotification> StockNotifications { get; }
    public IRepository<ProductQuestion> ProductQuestions { get; }
    public IRepository<BlogPost> BlogPosts { get; }
    public IRepository<BlogCategory> BlogCategories { get; }
    public IRepository<SupportTicket> SupportTickets { get; }
    public IRepository<TicketMessage> TicketMessages { get; }
    public IRepository<LoyaltyTransaction> LoyaltyTransactions { get; }
    public IRepository<ProductVariant> ProductVariants { get; }
    public IRepository<ReturnRequest> ReturnRequests { get; }
    public IRepository<ReturnItem> ReturnItems { get; }
    public IRepository<UserNotification> UserNotifications { get; }
    public IRepository<PriceRule> PriceRules { get; }
    public IRepository<Warehouse> Warehouses { get; }
    public IRepository<WarehouseStock> WarehouseStocks { get; }
    public IRepository<PaymentTransaction> PaymentTransactions { get; }
    public IRepository<ShipmentTracking> ShipmentTrackings { get; }
    public IRepository<ShipmentTrackingEvent> ShipmentTrackingEvents { get; }
    public IRepository<ProductView> ProductViews { get; }
    public IRepository<GiftCard> GiftCards { get; }
    public IRepository<GiftCardTransaction> GiftCardTransactions { get; }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
