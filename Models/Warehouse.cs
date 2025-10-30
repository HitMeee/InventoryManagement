using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Models
{
    [Table("warehouses")]
    public class Warehouse
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Owner of the warehouse (a user with role 'owner' for this warehouse)
        // An owner can own many warehouses, but each warehouse has exactly one owner.
        [Column("owner_id")]
        public int? OwnerId { get; set; }

        // Navigation properties
        public List<Product>? Products { get; set; }
        public List<UserWarehouseRole>? UserWarehouseRoles { get; set; }
    }
}
