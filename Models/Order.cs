using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public List<OrderItem>? Items { get; set; }
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
