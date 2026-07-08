using Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest;
using Web.BlazorServer.Components.Pages.Transaction.TripTicket;
using Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Components.Pages.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.Components.Pages.Transaction.InventoryCounting;
using Web.BlazorServer.Components.Pages.Administrator.Settings;

namespace Web.BlazorServer.Helpers;

public class NavRoutesRepository
{
    private static Lazy<NavRoutesRepository> _instance = new Lazy<NavRoutesRepository>(() => new NavRoutesRepository());
    public static NavRoutesRepository Instance => _instance.Value;

    public List<NavigationRouteVM> Roots = [];
    private NavigationRouteVM DashboardRoute;

    Dictionary<string, NavigationRouteVM> navRoutes = new();
    private NavRoutesRepository() 
    {
        DashboardRoute = new() { Name = "Dashboard", Icon = "dashboard", Protected = true, Uri="/dashboard" };
        initRoutes();
    }


    private void initRoutes()
    {
        NavigationRouteVM admin = new() { Name = "Administration", Icon = "discover_tune", Protected = false  };
        NavigationRouteVM transactions = new() { Name = "Transaction", Icon = "contract", Protected = false  };

        var adminSubroutes = initAdminRoutes();
        admin.Children.AddRange(adminSubroutes);
        adminSubroutes.ForEach(x => x.Parent = admin);

        var transactionsSubroutes = initTransactionRoutes();
        transactions.Children.AddRange(transactionsSubroutes);
        transactionsSubroutes.ForEach(x => x.Parent = transactions);

        Roots = [DashboardRoute, admin, transactions];
    }

    private List<NavigationRouteVM> initAdminRoutes()
    {
        NavigationRouteVM user = new() { Name = "User", Icon = "manage_accounts", Protected = false };
        NavigationRouteVM settings = new() { Name = "Settings", Icon = "settings", Protected = false };

        NavigationRouteVM syscon = Register("OSTN", new() { Name = "System Configuration", Icon = "build_circle", Protected = true, Uri=SystemConfiguration.ROUTE_INDEX });

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
        NavigationRouteVM delivery = new() { Name = "Delivery", Icon = "local_shipping", Protected = false };

        List<NavigationRouteVM> inventorySubroutes = [
            new() {Name = "Inventory Counting", Icon="home_storage", Protected=true, Uri = "/transactions/inventory/inventory-counting" },
            new() {Name = "Inventory Worksheet", Icon="home_storage", Protected=true, Uri = "/transactions/inventory/inventory-worksheet" },
        ];

        List<NavigationRouteVM> purchaseSubroutes = [
            Register("ORCV", new() {Name = "Receiving", Icon="stacked_inbox", Protected=true, Uri = "/transactions/purchasing/receiving" }),
            Register("ORDN",new() {Name = "Return to Supplier", Icon="assignment_return", Protected=true, Uri = SupplierReturnRoutes.INDEX })
        ];

        List<NavigationRouteVM> deliverySubroutes = [
            Register("OPCK", new() {Name = "Packing", Icon="package_2", Protected=true, Uri = PackingRoutes.Root}),
            Register("OTTX", new() {Name = "Trip Ticket", Icon="transit_ticket", Protected=true, Uri = TripTicketRoutes.Root}),
        ];
        
        purchaseSubroutes.ForEach(x => x.Parent = purchase);
        purchase.Children.AddRange(purchaseSubroutes);

        inventorySubroutes.ForEach(x => x.Parent = inventory);
        inventory.Children.AddRange(inventorySubroutes);

        deliverySubroutes.ForEach(x => x.Parent = delivery);
        delivery.Children.AddRange(deliverySubroutes);

        return [purchase, inventory, delivery];
    }

    public List<NavigationRouteVM> GetPath(string moduleCode)
    {
        if (!navRoutes.ContainsKey(moduleCode)) return [DashboardRoute];
        List<NavigationRouteVM> result = [navRoutes[moduleCode]];
        bool dashIncluded = false;
        while (result[0].Parent != null)
        {
            var top = result[0].Parent!;
            result.Insert(0, top);
            dashIncluded = dashIncluded || top == DashboardRoute;
        }

        if (!dashIncluded) result.Insert(0, DashboardRoute);
        

        return result;
    }

    private NavigationRouteVM Register(string moduleCode, NavigationRouteVM navRoute)
    {
        navRoutes.TryAdd(moduleCode, navRoute);
        return navRoute;
    }
}
