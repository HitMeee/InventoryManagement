# InventoryManagement (WPF + SQLite) — Starter

This folder contains a starter WPF (MVVM) application wired to SQLite via Entity Framework Core. It includes models, DbContext, a DB initializer with sample data, and a sample Products module (View/ViewModel/Service).

Important: If your path contains special characters (for example `#` in `GKC#`), some dotnet CLI commands may fail. If you encounter path errors when building or running, rename the parent folder to remove special characters (recommended) or open the project in Visual Studio and run from there.

Quick run (PowerShell) — from this folder:

```powershell
# restore and build
dotnet restore .\InventoryManagement.csproj
dotnet build .\InventoryManagement.csproj

# run (will open the WPF window)
dotnet run --project .\InventoryManagement.csproj
```

What to test first
- The app seeds sample data (Products, Warehouses, Users) on first startup (creates `inventory.db`).
- The main window hosts the Products view. Test: Add, Update, Delete product; refresh list.

If build fails
- Ensure you have .NET SDK that supports net8.0 (or edit `InventoryManagement.csproj` target to a version you have).
- If packages fail to restore, run `dotnet nuget locals all --clear` and retry.

Next steps
- I can implement the rest of modules (Inventory, Suppliers, Customers, Orders) with the same pattern. Tell me to continue and I will implement the next module and provide test steps.
