using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace InventoryManagement.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("password")]
        public string PasswordHash { get; set; } = string.Empty;

        [NotMapped]
        public string Role { get; set; } = string.Empty;

    public List<UserWarehouseRole>? UserWarehouseRoles { get; set; }
        
        [NotMapped]
        public string? PlainPassword { get; set; }
    }
}
