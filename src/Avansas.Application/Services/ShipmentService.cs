using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class ShipmentService : IShipmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ShipmentService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<ShipmentTrackingDto?> GetTrackingByOrderIdAsync(int orderId)
    {
        var tracking = await _unitOfWork.ShipmentTrackings.Query()
            .Include(s => s.Order)
            .Include(s => s.Events.Where(e => !e.IsDeleted).OrderByDescending(e => e.EventDate))
            .FirstOrDefaultAsync(s => s.OrderId == orderId);

        return tracking == null ? null : MapToDto(tracking);
    }

    public async Task<ShipmentTrackingDto?> GetTrackingByNumberAsync(string trackingNumber)
    {
        var tracking = await _unitOfWork.ShipmentTrackings.Query()
            .Include(s => s.Order)
            .Include(s => s.Events.Where(e => !e.IsDeleted).OrderByDescending(e => e.EventDate))
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);

        return tracking == null ? null : MapToDto(tracking);
    }

    public async Task<int> CreateShipmentAsync(CreateShipmentDto dto)
    {
        var tracking = new ShipmentTracking
        {
            OrderId = dto.OrderId,
            CargoCompany = dto.CargoCompany,
            TrackingNumber = dto.TrackingNumber,
            ShippedAt = DateTime.UtcNow,
            EstimatedDeliveryAt = dto.EstimatedDeliveryAt,
            CurrentStatus = ShipmentStatus.PickedUp
        };
        await _unitOfWork.ShipmentTrackings.AddAsync(tracking);

        // İlk event ekle
        var initialEvent = new ShipmentTrackingEvent
        {
            Status = ShipmentStatus.PickedUp,
            Description = "Kargo teslim alındı",
            EventDate = DateTime.UtcNow
        };
        await _unitOfWork.ShipmentTrackingEvents.AddAsync(initialEvent);

        // Sipariş durumunu güncelle
        var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId);
        if (order != null)
        {
            order.CargoTrackingNumber = dto.TrackingNumber;
            order.CargoCompany = dto.CargoCompany.ToString();
            order.ShippedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);

            // Kullanıcıya bildirim
            await _notificationService.CreateNotificationAsync(order.UserId, "Siparişiniz Kargoya Verildi",
                $"#{order.OrderNumber} numaralı siparişiniz {GetCompanyName(dto.CargoCompany)} kargo ile gönderildi. Takip No: {dto.TrackingNumber}", Domain.Enums.NotificationType.OrderStatus, "/hesap/siparisler");
        }

        await _unitOfWork.SaveChangesAsync();

        // Shipment event'e doğru ID'yi ata
        initialEvent.ShipmentTrackingId = tracking.Id;
        _unitOfWork.ShipmentTrackingEvents.Update(initialEvent);
        await _unitOfWork.SaveChangesAsync();

        return tracking.Id;
    }

    public async Task AddTrackingEventAsync(int shipmentId, ShipmentTrackingEventDto eventDto)
    {
        var ev = new ShipmentTrackingEvent
        {
            ShipmentTrackingId = shipmentId,
            Status = eventDto.Status,
            Description = eventDto.Description,
            Location = eventDto.Location,
            EventDate = eventDto.EventDate
        };
        await _unitOfWork.ShipmentTrackingEvents.AddAsync(ev);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateShipmentStatusAsync(int shipmentId, ShipmentStatus status, string? location = null)
    {
        var tracking = await _unitOfWork.ShipmentTrackings.GetByIdAsync(shipmentId);
        if (tracking == null) return;

        tracking.CurrentStatus = status;
        if (location != null) tracking.LastLocation = location;
        _unitOfWork.ShipmentTrackings.Update(tracking);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAsDeliveredAsync(int shipmentId, string? receiverName = null)
    {
        var tracking = await _unitOfWork.ShipmentTrackings.Query()
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (tracking == null) return;

        tracking.CurrentStatus = ShipmentStatus.Delivered;
        tracking.DeliveredAt = DateTime.UtcNow;
        if (receiverName != null) tracking.ReceiverName = receiverName;
        _unitOfWork.ShipmentTrackings.Update(tracking);

        var order = tracking.Order;
        if (order != null)
        {
            order.DeliveredAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);

            await _notificationService.CreateNotificationAsync(order.UserId, "Siparişiniz Teslim Edildi",
                $"#{order.OrderNumber} numaralı siparişiniz teslim edildi.", Domain.Enums.NotificationType.OrderStatus, "/hesap/siparisler");
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public string GetCargoTrackingUrl(CargoCompany company, string trackingNumber)
    {
        return company switch
        {
            CargoCompany.YurticiKargo => $"https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code={trackingNumber}",
            CargoCompany.ArasKargo => $"https://www.araskargo.com.tr/arastir.aspx?takipno={trackingNumber}",
            CargoCompany.MNGKargo => $"https://www.mngkargo.com.tr/gonderi-takip/?code={trackingNumber}",
            CargoCompany.PTTKargo => $"https://gonderitakip.ptt.gov.tr/Track/Verify?q={trackingNumber}",
            CargoCompany.SuratKargo => $"https://www.suratkargo.com.tr/KargoBilgileri/KargoSorgula?barcode={trackingNumber}",
            CargoCompany.UPSKargo => $"https://www.ups.com/track?tracknum={trackingNumber}",
            _ => "#"
        };
    }

    public async Task<List<ShipmentTrackingDto>> GetShipmentsAsync(ShipmentStatus? status = null, int page = 1, int pageSize = 20)
    {
        var query = _unitOfWork.ShipmentTrackings.Query()
            .Include(s => s.Order)
            .Include(s => s.Events.Where(e => !e.IsDeleted));

        if (status.HasValue)
            query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<ShipmentTracking, ICollection<ShipmentTrackingEvent>>)query.Where(s => s.CurrentStatus == status.Value);

        var list = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    private ShipmentTrackingDto MapToDto(ShipmentTracking s) => new(
        s.Id,
        s.OrderId,
        s.Order?.OrderNumber ?? "",
        s.CargoCompany,
        GetCompanyName(s.CargoCompany),
        s.TrackingNumber,
        s.ShippedAt,
        s.EstimatedDeliveryAt,
        s.DeliveredAt,
        s.CurrentStatus,
        GetStatusText(s.CurrentStatus),
        s.LastLocation,
        GetCargoTrackingUrl(s.CargoCompany, s.TrackingNumber),
        s.Events.Select(e => new ShipmentTrackingEventDto(e.Status, e.Description, e.Location, e.EventDate)).ToList()
    );

    private static string GetCompanyName(CargoCompany company) => company switch
    {
        CargoCompany.YurticiKargo => "Yurtiçi Kargo",
        CargoCompany.ArasKargo => "Aras Kargo",
        CargoCompany.MNGKargo => "MNG Kargo",
        CargoCompany.PTTKargo => "PTT Kargo",
        CargoCompany.SuratKargo => "Sürat Kargo",
        CargoCompany.UPSKargo => "UPS Kargo",
        _ => "Diğer Kargo"
    };

    private static string GetStatusText(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Preparing => "Hazırlanıyor",
        ShipmentStatus.PickedUp => "Kargo Teslim Alındı",
        ShipmentStatus.InTransit => "Yolda",
        ShipmentStatus.OutForDelivery => "Dağıtımda",
        ShipmentStatus.Delivered => "Teslim Edildi",
        ShipmentStatus.ReturnedToSender => "İade Edildi",
        ShipmentStatus.Failed => "Teslim Edilemedi",
        _ => "Bilinmiyor"
    };
}
