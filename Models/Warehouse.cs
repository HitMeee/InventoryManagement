using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public List<InventoryItem>? InventoryItems { get; set; }
    }
}
