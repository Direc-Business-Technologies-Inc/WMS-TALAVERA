using Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;
using Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Helpers;

public class NavRoutesRepository
{
    private static Lazy<NavRoutesRepository> _instance = new Lazy<NavRoutesRepository>(() => new NavRoutesRepository());
    public static NavRoutesRepository Instance => _instance.Value;

    public List<NavigationRouteVM> Roots = [];

    Dictionary<string, NavigationRouteVM> navDict = new();
    private NavRoutesRepository() 
    {
        initRoutes();
    }


    private void initRoutes()
    {
        NavigationRouteVM admin = new() { Name = "Administration", Icon = "discover_tune", Protected = false  };
        NavigationRouteVM transactions = new() { Name = "Transaction", Icon = "contract", Protected = false  };
        NavigationRouteVM dash = new() { Name = "Dashboard", Icon = "dashboard", Protected = true, Uri="/dashboard" };

        var adminSubroutes = initAdminRoutes();
        admin.Children.AddRange(adminSubroutes);
        adminSubroutes.ForEach(x => x.Parent = admin);

        var transactionsSubroutes = initTransactionRoutes();
        transactions.Children.AddRange(transactionsSubroutes);
        transactionsSubroutes.ForEach(x => x.Parent = transactions);

        Roots = [dash, admin, transactions];
    }

    private List<NavigationRouteVM> initAdminRoutes()
    {
        NavigationRouteVM user = new() { Name = "User", Icon = "manage_accounts", Protected = false };
        NavigationRouteVM settings = new() { Name = "Settings", Icon = "settings", Protected = false };

        NavigationRouteVM syscon = new() { Name = "System Configuration", Icon = "build_circle", Protected = true, Uri= "/administration/settings/system-configuration" };

        NavigationRouteVM uauth = new() { Name = "User Authorization", Icon = "admin_panel_settings", Protected = true, Uri= "/administration/user/authorization-management" };
        NavigationRouteVM uman = new() { Name = "User Management", Icon = "account_circle", Protected = true, Uri= "/administration/user/user-management" };
        NavigationRouteVM urole = new() { Name = "User Roles", Icon = "groups", Protected = true, Uri= "/administration/user/role-management" };

        List<NavigationRouteVM> userSubroutes = [uauth, uman, urole];

        userSubroutes.ForEach(x => x.Parent = user);
        user.Children.AddRange(userSubroutes);

        settings.Children.Add(syscon);
        syscon.Parent = settings;

        return [user, settings];
    }

    private List<NavigationRouteVM> initTransactionRoutes()
    {
        NavigationRouteVM purchase = new() { Name = "Purchasing A/P", Icon = "archive", Protected = false };
        NavigationRouteVM inventory = new() { Name = "Inventory", Icon = "inventory_2", Protected = false };

        List<NavigationRouteVM> inventorySubroutes = [
            new() {Name = "Stock Transfer Request", Icon="battery_android_share", Protected=true, Uri = STRRoutes.Root },
            new() {Name = "Inventory Adjustment", Icon="swap_vert", Protected=true, Uri = InventoryAdjustmentRoutes.INDEX },
            new() {Name = "Inventory Transfer Request", Icon="warehouse", Protected=true, Uri = ITRRoutes.INDEX },
        ];

        List<NavigationRouteVM> purchaseSubroutes = [
            new() {Name = "Receiving", Icon="stacked_inbox", Protected=true, Uri = "/transactions/purchasing/receiving" },
        ];

        purchaseSubroutes.ForEach(x => x.Parent = purchase);
        purchase.Children.AddRange(purchaseSubroutes);

        inventorySubroutes.ForEach(x => x.Parent = inventory);
        inventory.Children.AddRange(inventorySubroutes);

        return [purchase, inventory];
    }

}
