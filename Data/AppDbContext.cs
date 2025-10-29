using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data
{
    public class AppDbContext : DbContext
    {
        private readonly string _connectionString;

        public AppDbContext(string? connectionString = null)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _connectionString = connectionString;
            }
            else
            {
                try
                {
                    var baseDir = AppContext.BaseDirectory;
                    var dir = new System.IO.DirectoryInfo(baseDir);
                    string? projectRoot = null;
                    for (int i = 0; i < 8 && dir != null; i++)
                    {
                        var csproj = System.IO.Path.Combine(dir.FullName, "InventoryManagement.csproj");
                        if (System.IO.File.Exists(csproj))
                        {
                            projectRoot = dir.FullName;
                            break;
                        }
                        dir = dir.Parent;
                    }

                    if (!string.IsNullOrEmpty(projectRoot))
                    {
                        var candidate = System.IO.Path.Combine(projectRoot, "inventory.db");
                        if (System.IO.File.Exists(candidate))
                        {
                            _connectionString = $"Data Source={candidate}";
                        }
                        else
                        {
                            _connectionString = $"Data Source={System.IO.Path.Combine(projectRoot, "inventory.db")}";
                        }
                    }
                    else
                    {
                        _connectionString = "Data Source=inventory.db";
                    }
                }
                catch
                {
                    _connectionString = "Data Source=inventory.db";
                }
            }
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }
    public DbSet<UserWarehouseRole> UserWarehouseRoles { get; set; }
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

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Warehouse>().ToTable("warehouses");

            modelBuilder.Entity<UserWarehouseRole>()
                .HasOne(uw => uw.User).WithMany(u => u.UserWarehouseRoles).HasForeignKey(uw => uw.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserWarehouseRole>().ToTable("user_warehouse_roles");

            modelBuilder.Entity<UserWarehouseRole>()
                .HasOne(uw => uw.Warehouse).WithMany().HasForeignKey(uw => uw.WarehouseId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserWarehouseRole>()
                .HasIndex(uw => new { uw.WarehouseId, uw.Role }).IsUnique();

            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Product).WithMany(p => p.InventoryItems).HasForeignKey(ii => ii.ProductId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Warehouse).WithMany(w => w.InventoryItems).HasForeignKey(ii => ii.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
