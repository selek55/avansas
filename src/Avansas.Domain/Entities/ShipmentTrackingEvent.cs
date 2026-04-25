using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class ShipmentTrackingEvent : BaseEntity
{
    public int ShipmentTrackingId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime EventDate { get; set; } = DateTime.UtcNow;

    public ShipmentTracking ShipmentTracking { get; set; } = null!;
}
