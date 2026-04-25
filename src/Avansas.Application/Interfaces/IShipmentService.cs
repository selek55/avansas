using Avansas.Domain.Enums;

namespace Avansas.Application.Interfaces;

public record ShipmentTrackingEventDto(
    ShipmentStatus Status,
    string Description,
    string? Location,
    DateTime EventDate);

public record CreateShipmentDto(
    int OrderId,
    CargoCompany CargoCompany,
    string TrackingNumber,
    DateTime? EstimatedDeliveryAt);

public record ShipmentTrackingDto(
    int Id,
    int OrderId,
    string OrderNumber,
    CargoCompany CargoCompany,
    string CargoCompanyName,
    string TrackingNumber,
    DateTime ShippedAt,
    DateTime? EstimatedDeliveryAt,
    DateTime? DeliveredAt,
    ShipmentStatus CurrentStatus,
    string StatusText,
    string? LastLocation,
    string TrackingUrl,
    List<ShipmentTrackingEventDto> Events);

public interface IShipmentService
{
    Task<ShipmentTrackingDto?> GetTrackingByOrderIdAsync(int orderId);
    Task<ShipmentTrackingDto?> GetTrackingByNumberAsync(string trackingNumber);
    Task<int> CreateShipmentAsync(CreateShipmentDto dto);
    Task AddTrackingEventAsync(int shipmentId, ShipmentTrackingEventDto eventDto);
    Task UpdateShipmentStatusAsync(int shipmentId, ShipmentStatus status, string? location = null);
    Task MarkAsDeliveredAsync(int shipmentId, string? receiverName = null);
    string GetCargoTrackingUrl(CargoCompany company, string trackingNumber);
    Task<List<ShipmentTrackingDto>> GetShipmentsAsync(ShipmentStatus? status = null, int page = 1, int pageSize = 20);
}
