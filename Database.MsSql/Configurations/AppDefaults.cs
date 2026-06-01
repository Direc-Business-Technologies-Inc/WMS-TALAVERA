using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.System;
using Domain.ValueObjects.Others;
using DataCipher;

namespace Database.MsSql.Configurations;

internal static class AppDefaults
{
    public static PermissionVO NewPermission(string name) => new(name);

    public static readonly IEnumerable<PermissionVO> Permissions =
    [
        new PermissionVO("CREATE"),
        new PermissionVO("VIEW"),
        new PermissionVO("UPDATE"),
        new PermissionVO("ARCHIVE"),
        //new PermissionVO("APPROVE"),
        //new PermissionVO("ADMIN"),
    ];

    public static readonly IEnumerable<ModuleDEM> ModuleList =
    [
        ModuleDEM.Create(
            name: "Dashboard",
            code: "ODSB",
            root: false,
            navRouteId: WebStructure.ParentRouteList.First(x => x.Name.Equals("Dashboard")).Id,
            permissions: [AppDefaults.NewPermission("VIEW")]),
        ModuleDEM.Create(
            name: "User Management",
            code: "OUSR",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("User Management")).Id,
            permissions: [AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("UPDATE"), AppDefaults.NewPermission("ARCHIVE")]),
        ModuleDEM.Create(
            name: "User Roles",
            code: "OROL",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("User Roles")).Id,
            permissions: [AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("UPDATE"), AppDefaults.NewPermission("ARCHIVE")]),
        ModuleDEM.Create(
            name: "User Authorization",
            code: "OAUT",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("User Authorization")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"),  AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "System Configuration",
            code: "OSYS",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("System Configuration")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Receiving",
            code: "ORCV",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Receiving")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Goods Return",
            code: "OGRN",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Goods Return")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Delivery",
            code: "ODLV",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Delivery")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Sales Return",
            code: "OSRN",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Sales Return")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Goods Issue",
            code: "OGIS",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Goods Issue")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Goods Receipt",
            code: "OGRC",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Goods Receipt")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Inventory Transfer",
            code: "OITR",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Inventory Transfer")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
        ModuleDEM.Create(
            name: "Inventory Counting",
            code: "OICT",
            root: false,
            navRouteId: WebStructure.SubRouteList2.First(x => x.Name.Equals("Inventory Counting")).Id,
            permissions: [AppDefaults.NewPermission("VIEW"), AppDefaults.NewPermission("CREATE"), AppDefaults.NewPermission("UPDATE")]),
    ];

    public static readonly IEnumerable<RoleDEM> Roles =
    [
        RoleDEM.Create("ROOT", "ROOT"),
        RoleDEM.Create("SYS_ADMIN", "Administrator"),
        RoleDEM.Create("WHS_STAFF", "Warehouse Staff"),
        RoleDEM.Create("FINANCE", "Finance"),
    ];

    public static readonly IEnumerable<UserDEM> Users =
    [
        UserDEM.Create(new (new("System"), string.Empty, "Administrator"),
                       new("email@address.com"),
                       new(new("WMS-0001"), Encryption.Encrypt("B1Admin!"), false),
                       "APHI",
                       Roles.ElementAt(1).Id,
                       "09995638664"),
        UserDEM.Create(new (new("Warehouse"), string.Empty, "Staff"),
                       new("email@address.com"),
                       new(new("WMS-0002"), Encryption.Encrypt("B1Admin!"), false),
                       "APHI",
                       Roles.ElementAt(2).Id,
                       "09995638664"),
        UserDEM.Create(new (new("Finance"), string.Empty, "Staff"),
                       new("email@address.com"),
                       new(new("WMS-0003"), Encryption.Encrypt("B1Admin!"), false),
                       "APHI",
                       Roles.ElementAt(3).Id,
                       "09995638664"),
    ];

    public static IEnumerable<ModuleDEM> RoleModules(RoleDEM role)
    {

        return role.Code switch
        {
            "SYS_ADMIN" => [.. ModuleList],
            "WHS_STAFF" => [.. ModuleList.Where(m => m.Code is "ODSB" or "ORCV" or "OGRN" or "ODLV" or "OSRN" or "OGIS" or "OGRC" or "OITR" or "OICT")],
            "FINANCE" => [.. ModuleList.Where(m => m.Code is "ODSB" or "ORCV" or "OGRN" or "ODLV" or "OSRN" or "OGIS" or "OGRC")],
            _ => []
        };
    }

}
