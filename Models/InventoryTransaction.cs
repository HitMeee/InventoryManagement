using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Models
{
    [Table("inventory_transactions")]
    public class InventoryTransaction
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("warehouse_id")]
        public int WarehouseId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("transaction_type")]
        [Required, MaxLength(10)]
        public string TransactionType { get; set; } = string.Empty; // "IMPORT" or "EXPORT"

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unit")]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        [Column("note")]
        [MaxLength(500)]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product? Product { get; set; }
        public Warehouse? Warehouse { get; set; }
        public User? User { get; set; }
    }
}