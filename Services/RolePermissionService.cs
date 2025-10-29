using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryManagement.Services
{
    public static class RolePermissionService
    {
        public static class Features
        {
            public const string ManageUsers = "ManageUsers";
            public const string ManageCustomers = "ManageCustomers";
            public const string ManageProducts = "ManageProducts";
            public const string SearchProducts = "SearchProducts";
            public const string ManageInventory = "ManageInventory";
            public const string LowStockAlert = "LowStockAlert";
            public const string ViewStock = "ViewStock";
            public const string ManageOrders = "ManageOrders";
            public const string ViewInOutHistory = "ViewInOutHistory";
            public const string ViewStockReports = "ViewStockReports";
            public const string ViewSalesReports = "ViewSalesReports";
            public const string ExportReports = "ExportReports";
            public const string SystemConfig = "SystemConfig";
            public const string Logout = "Logout";
        }

        private static readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Chủ kho"] = new HashSet<string>
            {
                Features.ManageUsers,
                Features.ManageProducts,
                Features.SearchProducts,
                Features.ManageInventory,
                Features.LowStockAlert,
                Features.ViewStock,
                Features.ManageOrders,
                Features.ViewInOutHistory,
                Features.ViewStockReports,
                Features.ViewSalesReports,
                Features.ExportReports,
                Features.SystemConfig,
                Features.Logout
            },
            ["Admin"] = new HashSet<string>
            {
                Features.ManageUsers,
                Features.ManageProducts,
                Features.SearchProducts,
                Features.ManageInventory,
                Features.LowStockAlert,
                Features.ViewStock,
                Features.ManageOrders,
                Features.ViewInOutHistory,
                Features.ViewStockReports,
                Features.ViewSalesReports,
                Features.ExportReports,
                Features.SystemConfig,
                Features.Logout
            },
            ["Nhân viên kho"] = new HashSet<string>
            {
                Features.ManageProducts,
                Features.SearchProducts,
                Features.ManageInventory,
                Features.LowStockAlert,
                Features.ViewStock,
                Features.ViewInOutHistory,
                Features.ViewStockReports,
                Features.ExportReports,
                Features.Logout
            },
            ["Nhân viên bán hàng"] = new HashSet<string>
            {
                Features.SearchProducts,
                Features.ManageCustomers,
                Features.ViewStock,
                Features.ManageOrders,
                Features.ViewSalesReports,
                Features.ExportReports,
                Features.Logout
            }
        };

        public static bool HasPermission(string? role, string feature)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            if (string.IsNullOrWhiteSpace(feature)) return false;
            if (!_map.TryGetValue(role.Trim(), out var set)) return false;
            return set.Contains(feature);
        }

        public static IEnumerable<string> GetAllowedFeatures(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return Enumerable.Empty<string>();
            if (_map.TryGetValue(role.Trim(), out var set)) return set;
            return Enumerable.Empty<string>();
        }
    }
}
