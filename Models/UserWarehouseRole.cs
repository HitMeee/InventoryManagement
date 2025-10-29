using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Models
{
    [Table("user_warehouse_roles")]
    public class UserWarehouseRole
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Column("warehouse_id")]
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        [Column("role")]
        public string Role { get; set; } = string.Empty; // 'admin' or 'staff'

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}