using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models
{
    // Role stored as string for flexibility ("Admin", "Kho", "BanHang")
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // in demo stored plain; hash in production
        public string Role { get; set; } = "Admin";
    }
}
