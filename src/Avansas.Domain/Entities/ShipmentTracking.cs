using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class ShipmentTracking : BaseEntity
{
    public int OrderId { get; set; }
    public CargoCompany CargoCompany { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime ShippedAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public ShipmentStatus CurrentStatus { get; set; } = ShipmentStatus.Preparing;
    public string? LastLocation { get; set; }
    public string? ReceiverName { get; set; }

    public Order Order { get; set; } = null!;
    public ICollection<ShipmentTrackingEvent> Events { get; set; } = new List<ShipmentTrackingEvent>();
}
