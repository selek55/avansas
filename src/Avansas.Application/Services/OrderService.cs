using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Avansas.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly decimal _freeShippingThreshold;
    private readonly decimal _shippingCost;

    public OrderService(IUnitOfWork uow, UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration)
    {
        _uow = uow;
        _userManager = userManager;
        _emailService = emailService;
        _freeShippingThreshold = configuration.GetValue<decimal>("AppSettings:FreeShippingThreshold", 500m);
        _shippingCost = configuration.GetValue<decimal>("AppSettings:ShippingCost", 29.90m);
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 20, OrderStatus? status = null)
    {
        var query = _uow.Orders.Query()
            .Include(o => o.Items).Include(o => o.User)
            .Where(o => !o.IsDeleted);

        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        query = query.OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total, PageNumber = page, PageSize = pageSize
        };
    }

    public async Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 10)
    {
        var query = _uow.Orders.Query()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total, PageNumber = page, PageSize = pageSize
        };
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && !o.IsDeleted);
        return order == null ? null : MapToDto(order);
    }

    public async Task<int> CreateOrderFromCartAsync(CreateOrderDto dto)
    {
        var cart = await _uow.Carts.Query()
            .Include(c => c.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == dto.UserId && !c.IsDeleted);

        if (cart == null || !cart.Items.Any(i => !i.IsDeleted))
            throw new InvalidOperationException("Sepet boş");

        var shippingAddress = await _uow.Addresses.GetByIdAsync(dto.ShippingAddressId)
            ?? throw new KeyNotFoundException("Teslimat adresi bulunamadı");
        var billingAddress = await _uow.Addresses.GetByIdAsync(dto.BillingAddressId)
            ?? throw new KeyNotFoundException("Fatura adresi bulunamadı");

        var subTotal = cart.Items.Where(i => !i.IsDeleted).Sum(i => i.TotalPrice);
        var shippingCost = await CalculateShippingCostAsync(subTotal);
        var taxAmount = cart.Items.Where(i => !i.IsDeleted)
            .Sum(i => i.TotalPrice * (i.Product!.TaxRate / 100));
        var total = subTotal + shippingCost - cart.DiscountAmount;

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = dto.UserId, PaymentMethod = dto.PaymentMethod,
            SubTotal = subTotal, ShippingCost = shippingCost,
            DiscountAmount = cart.DiscountAmount, TaxAmount = taxAmount,
            Total = total, CouponCode = cart.CouponCode, Note = dto.Note,
            ShippingFirstName = shippingAddress.FirstName, ShippingLastName = shippingAddress.LastName,
            ShippingPhone = shippingAddress.Phone, ShippingAddress = shippingAddress.AddressLine1,
            ShippingCity = shippingAddress.City, ShippingDistrict = shippingAddress.District,
            ShippingPostalCode = shippingAddress.PostalCode,
            BillingFirstName = billingAddress.FirstName, BillingLastName = billingAddress.LastName,
            BillingCompanyName = billingAddress.CompanyName, BillingAddress = billingAddress.AddressLine1,
            BillingCity = billingAddress.City, BillingTaxNumber = billingAddress.TaxNumber,
            BillingTaxOffice = billingAddress.TaxOffice,
            Status = dto.PaymentMethod == PaymentMethod.CashOnDelivery ? OrderStatus.Confirmed : OrderStatus.Pending,
            PaymentStatus = dto.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Pending : PaymentStatus.Pending
        };

        await _uow.Orders.AddAsync(order);
        await _uow.SaveChangesAsync();

        foreach (var item in cart.Items.Where(i => !i.IsDeleted))
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id, ProductId = item.ProductId,
                ProductName = item.Product!.Name, ProductSKU = item.Product.SKU,
                ProductImageUrl = item.Product.MainImageUrl, Quantity = item.Quantity,
                UnitPrice = item.UnitPrice, TaxRate = item.Product.TaxRate
            };
            await _uow.OrderItems.AddAsync(orderItem);

            item.Product.StockQuantity -= item.Quantity;
            _uow.Products.Update(item.Product);
        }

        if (!string.IsNullOrEmpty(cart.CouponCode))
        {
            var coupon = await _uow.Coupons.Query().FirstOrDefaultAsync(c => c.Code == cart.CouponCode);
            if (coupon != null) { coupon.UsedCount++; _uow.Coupons.Update(coupon); }
        }

        foreach (var item in cart.Items.Where(i => !i.IsDeleted)) _uow.CartItems.SoftDelete(item);
        cart.CouponCode = null; cart.DiscountAmount = 0;
        _uow.Carts.Update(cart);

        await _uow.SaveChangesAsync();

        // Sipariş onay e-postası gönder (müşteriye + admin'e)
        var orderDto = await GetOrderByIdAsync(order.Id);
        if (orderDto != null)
        {
            _ = Task.Run(() => _emailService.SendOrderConfirmationAsync(orderDto));
            _ = Task.Run(() => _emailService.SendNewOrderNotificationToAdminAsync(orderDto));
        }

        return order.Id;
    }

    public async Task UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
    {
        var order = await _uow.Orders.GetByIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException($"Sipariş {dto.OrderId} bulunamadı");

        order.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.CargoTrackingNumber)) order.CargoTrackingNumber = dto.CargoTrackingNumber;
        if (!string.IsNullOrEmpty(dto.CargoCompany)) order.CargoCompany = dto.CargoCompany;
        if (dto.Status == OrderStatus.Shipped) order.ShippedAt = DateTime.UtcNow;
        if (dto.Status == OrderStatus.Delivered) { order.DeliveredAt = DateTime.UtcNow; order.PaymentStatus = PaymentStatus.Paid; }
        order.UpdatedAt = DateTime.UtcNow;

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync();

        // Durum güncelleme e-postası gönder
        var orderDto = await GetOrderByIdAsync(dto.OrderId);
        if (orderDto != null)
            _ = Task.Run(() => _emailService.SendOrderStatusUpdateAsync(orderDto));
    }

    public async Task CancelOrderAsync(int orderId, string userId)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) throw new KeyNotFoundException("Sipariş bulunamadı");
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Bu sipariş iptal edilemez");

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            if (item.Product != null) { item.Product.StockQuantity += item.Quantity; _uow.Products.Update(item.Product); }
        }

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync();
    }

    public Task<decimal> CalculateShippingCostAsync(decimal cartTotal) =>
        Task.FromResult(cartTotal >= _freeShippingThreshold ? 0m : _shippingCost);

    public async Task<Dictionary<string, object>> GetOrderStatisticsAsync()
    {
        var orders = await _uow.Orders.Query().Where(o => !o.IsDeleted).ToListAsync();
        return new Dictionary<string, object>
        {
            ["TotalOrders"] = orders.Count,
            ["PendingOrders"] = orders.Count(o => o.Status == OrderStatus.Pending),
            ["ProcessingOrders"] = orders.Count(o => o.Status == OrderStatus.Processing),
            ["ShippedOrders"] = orders.Count(o => o.Status == OrderStatus.Shipped),
            ["TodayRevenue"] = orders.Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date && o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.Total),
            ["MonthRevenue"] = orders.Where(o => o.CreatedAt.Month == DateTime.UtcNow.Month && o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.Total),
            ["TotalRevenue"] = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.Total)
        };
    }

    private static string GenerateOrderNumber() =>
        $"AV{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(10000, 99999)}";

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id, OrderNumber = o.OrderNumber, UserId = o.UserId,
        UserFullName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}" : string.Empty,
        UserEmail = o.User?.Email ?? string.Empty,
        Status = o.Status, StatusText = GetStatusText(o.Status),
        PaymentStatus = o.PaymentStatus, PaymentStatusText = GetPaymentStatusText(o.PaymentStatus),
        PaymentMethod = o.PaymentMethod, PaymentMethodText = GetPaymentMethodText(o.PaymentMethod),
        SubTotal = o.SubTotal, ShippingCost = o.ShippingCost, DiscountAmount = o.DiscountAmount,
        TaxAmount = o.TaxAmount, Total = o.Total, CouponCode = o.CouponCode,
        ShippingFirstName = o.ShippingFirstName, ShippingLastName = o.ShippingLastName,
        ShippingPhone = o.ShippingPhone, ShippingAddress = o.ShippingAddress,
        ShippingDistrict = o.ShippingDistrict, ShippingCity = o.ShippingCity,
        CargoTrackingNumber = o.CargoTrackingNumber, CargoCompany = o.CargoCompany,
        Note = o.Note, CreatedAt = o.CreatedAt, ShippedAt = o.ShippedAt, DeliveredAt = o.DeliveredAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id, ProductId = i.ProductId, ProductName = i.ProductName,
            ProductSKU = i.ProductSKU, ProductImageUrl = i.ProductImageUrl,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
        }).ToList()
    };

    private static string GetStatusText(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Bekliyor", OrderStatus.Confirmed => "Onaylandı",
        OrderStatus.Processing => "Hazırlanıyor", OrderStatus.Shipped => "Kargoya Verildi",
        OrderStatus.Delivered => "Teslim Edildi", OrderStatus.Cancelled => "İptal Edildi",
        OrderStatus.Refunded => "İade Edildi", _ => s.ToString()
    };

    private static string GetPaymentStatusText(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "Bekliyor", PaymentStatus.Paid => "Ödendi",
        PaymentStatus.Failed => "Başarısız", PaymentStatus.Refunded => "İade Edildi",
        _ => s.ToString()
    };

    private static string GetPaymentMethodText(PaymentMethod m) => m switch
    {
        PaymentMethod.CreditCard => "Kredi Kartı", PaymentMethod.BankTransfer => "Havale/EFT",
        PaymentMethod.CashOnDelivery => "Kapıda Ödeme", _ => m.ToString()
    };
}
