using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
    }
}
