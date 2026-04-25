namespace Avansas.Domain.Enums;

public enum ShipmentStatus
{
    Preparing = 0,
    PickedUp = 1,
    InTransit = 2,
    OutForDelivery = 3,
    Delivered = 4,
    ReturnedToSender = 5,
    Failed = 6
}
