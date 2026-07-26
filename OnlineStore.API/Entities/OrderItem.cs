namespace OnlineStore.API.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty; // snapshot at checkout
    public decimal UnitPrice { get; set; }                   // snapshot at checkout
    public int Quantity { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}
