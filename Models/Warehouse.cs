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
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        public string Location { get; set; } = string.Empty;

        public List<InventoryItem>? InventoryItems { get; set; }
    }
}
