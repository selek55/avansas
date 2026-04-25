namespace Avansas.Domain.Entities;

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal TotalPrice => UnitPrice * Quantity;
}
