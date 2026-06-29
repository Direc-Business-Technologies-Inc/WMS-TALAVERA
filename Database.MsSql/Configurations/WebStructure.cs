using Domain.Entities.System;

namespace Database.MsSql.Configurations;

public static class WebStructure
{
    public static readonly IEnumerable<NavigationRouteDEM> ParentRouteList =
    [
        NavigationRouteDEM.New(
            name: "Dashboard",
            protectedRoute: true,
            position: 0,
            icon: "dashboard",
            uri: "/dashboard"),
        NavigationRouteDEM.New(
            name: "Administration",
            position: 1,
            icon: "discover_tune"),

        NavigationRouteDEM.New(
            name: "Transaction",
            position: 2,
            icon: "contract"),
    ];

    public static readonly IEnumerable<NavigationRouteDEM> SubRouteList1 =
    [
        NavigationRouteDEM.New(
            name: "User",
            position: 0,
            icon: "manage_accounts",
            parentId: ParentRouteList!.First(x => x.Name.Equals("Administration")).Id),
        NavigationRouteDEM.New(
            name: "Settings",
            position: 1,
            icon: "settings",
            parentId: ParentRouteList!.First(x => x.Name.Equals("Administration")).Id),
        NavigationRouteDEM.New(
            name: "Purchasing A/P",
            position: 0,
            icon: "archive",
            parentId: ParentRouteList!.First(x => x.Name.Equals("Transaction")).Id),
        NavigationRouteDEM.New(
            name: "Sales A/R",
            position: 1,
            icon: "unarchive",
            parentId: ParentRouteList!.First(x => x.Name.Equals("Transaction")).Id),
        NavigationRouteDEM.New(
            name: "Inventory",
            position: 2,
            icon: "inventory_2",
            parentId: ParentRouteList!.First(x => x.Name.Equals("Transaction")).Id),
    ];

    public static readonly IEnumerable<NavigationRouteDEM> SubRouteList2 =
    [
        NavigationRouteDEM.New(
            name: "User Management",
            protectedRoute: true,
            position: 0,
            parentId: SubRouteList1!.First(x => x.Name.Equals("User")).Id,
            icon: "account_circle",
            uri: "/administration/user/user-management"),
        NavigationRouteDEM.New(
            name: "User Roles",
            protectedRoute : true, 
            position: 1,
            parentId: SubRouteList1!.First(x => x.Name.Equals("User")).Id,
            icon: "groups",
            uri: "/administration/user/role-management"),
        NavigationRouteDEM.New(
            name: "User Authorization",
            protectedRoute : true, 
            position: 2,
            parentId: SubRouteList1!.First(x => x.Name.Equals("User")).Id,
            icon: "admin_panel_settings",
            uri: "/administration/user/authorization-management"),
        NavigationRouteDEM.New(
            name: "System Configuration",
            protectedRoute : true, 
            position: 0,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Settings")).Id,
            icon: "build_circle",
            uri: "/administration/settings/system-configuration"),
        NavigationRouteDEM.New(
            name: "Receiving",
            protectedRoute : true, 
            position: 0,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Purchasing A/P")).Id,
            icon: "stacked_inbox",
            uri: "/transactions/purchasing/receiving"),
        NavigationRouteDEM.New(
            name: "Goods Return",
            protectedRoute : true, 
            position: 1,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Purchasing A/P")).Id,
            icon: "outbox",
            uri: "/transactions/purchasing/goods-return"),
        NavigationRouteDEM.New(
            name: "Delivery",
            protectedRoute : true,
            position: 0,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Sales A/R")).Id,
            icon: "local_shipping",
            uri: "/transactions/sales/delivery"),
        NavigationRouteDEM.New(
            name: "Sales Return",
            protectedRoute : true,
            position: 1,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Sales A/R")).Id,
            icon: "pallet",
            uri: "/transactions/sales/sales-return"),
        NavigationRouteDEM.New(
            name: "Goods Issue",
            protectedRoute : true,
            position: 0,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Inventory")).Id,
            icon: "bottom_panel_close",
            uri: "/transactions/inventory/goods-issue"),
        NavigationRouteDEM.New(
            name: "Goods Receipt",
            protectedRoute : true,
            position: 1,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Inventory")).Id,
            icon: "bottom_panel_open",
            uri: "/transactions/inventory/goods-receipt"),
        NavigationRouteDEM.New(
            name: "Inventory Transfer",
            protectedRoute : true,
            position: 2,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Inventory")).Id,
            icon: "battery_android_share",
            uri: "/transactions/inventory/inventory-transfer"),
        NavigationRouteDEM.New(
            name: "Inventory Counting",
            protectedRoute : true,
            position: 3,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Inventory")).Id,
            icon: "home_storage",
            uri: "/transactions/inventory/inventory-counting"),
        NavigationRouteDEM.New(
            name: "Packing",
            protectedRoute : true,
            position: 4,
            parentId: SubRouteList1!.First(x => x.Name.Equals("Inventory")).Id,
            icon: "package_2",
            uri: "/transactions/inventory/packing"),
    ];
}
