using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data
{
    public class AppDbContext : DbContext
    {
        private readonly string _connectionString;

        public AppDbContext(string? connectionString = null)
        {
            _connectionString = connectionString ?? "Data Source=inventory.db";
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().HasIndex(p => p.Code).IsUnique();

            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Product).WithMany(p => p.InventoryItems).HasForeignKey(ii => ii.ProductId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Warehouse).WithMany(w => w.InventoryItems).HasForeignKey(ii => ii.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
