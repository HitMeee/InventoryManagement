using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("supplier_id")]
    public int? SupplierId { get; set; }

        [Column("name")]
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column("quantity")]
        public int Quantity { get; set; } = 0;

        [Column("unit")]
        [MaxLength(50)]
        public string Unit { get; set; } = "cái";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Warehouse? Warehouse { get; set; }
        public Supplier? Supplier { get; set; }
    }
}
