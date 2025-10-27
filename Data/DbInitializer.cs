using InventoryManagement.Models;

namespace InventoryManagement.Data
{
    public static class DbInitializer
    {
        public static void Initialize(string? connectionString = null)
        {
            using var context = new AppDbContext(connectionString);
            context.Database.EnsureCreated();

            // seed products only if empty
            if (!context.Products.Any())
            {
                var p1 = new Product { Code = "P001", Name = "Widget A", Price = 12.50m, ReorderLevel = 5 };
                var p2 = new Product { Code = "P002", Name = "Widget B", Price = 8.75m, ReorderLevel = 10 };
                context.Products.AddRange(p1, p2);

                var w1 = new Warehouse { Name = "Main Warehouse", Location = "Hanoi" };
                var w2 = new Warehouse { Name = "Secondary", Location = "HCM" };
                context.Warehouses.AddRange(w1, w2);

                context.SaveChanges();

                context.InventoryItems.Add(new InventoryItem { ProductId = p1.Id, WarehouseId = w1.Id, Quantity = 20 });
                context.InventoryItems.Add(new InventoryItem { ProductId = p2.Id, WarehouseId = w1.Id, Quantity = 5 });

                context.Customers.Add(new Customer { Name = "ACME Corp", Phone = "0123456789", Address = "1 Street" });
                context.Suppliers.Add(new Supplier { Name = "Supplier X", Contact = "supplier@example.com" });

            }

            // ensure users exist (add or update seeded users so login works even if DB existed)
            var seeded = new List<User>
            {
                new User { Username = "admin", PasswordHash = "admin", Role = "Admin" },
                new User { Username = "nhanvienkho", PasswordHash = "kho123", Role = "Nhân viên kho" },
                new User { Username = "banhang", PasswordHash = "ban123", Role = "Nhân viên bán hàng" }
            };

            foreach (var su in seeded)
            {
                var existing = context.Users.FirstOrDefault(u => u.Username == su.Username);
                if (existing == null)
                {
                    context.Users.Add(su);
                }
                else
                {
                    // update password/role if different
                    var changed = false;
                    if (existing.PasswordHash != su.PasswordHash)
                    {
                        existing.PasswordHash = su.PasswordHash;
                        changed = true;
                    }
                    if (!string.Equals(existing.Role ?? string.Empty, su.Role ?? string.Empty, StringComparison.Ordinal))
                    {
                        existing.Role = su.Role;
                        changed = true;
                    }
                    if (changed)
                    {
                        context.Users.Update(existing);
                    }
                }
            }
            context.SaveChanges();
        }
    }
}
