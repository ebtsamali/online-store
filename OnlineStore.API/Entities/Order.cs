namespace OnlineStore.API.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "pending"; // "pending" | "paid" | "failed"
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
