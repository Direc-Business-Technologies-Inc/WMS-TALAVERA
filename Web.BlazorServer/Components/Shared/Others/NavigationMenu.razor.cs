using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Shared.Kernel;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Components.Shared.Others;

public partial class NavigationMenu : BaseComponent
{
    [Inject] INavigationRouteHandler NavigationRouteHandler { get; set; } = default!;
    [Inject] AppAuthenticationService AppAuthenticationService { get; set; } = default!;
    List<NavigationRouteVM> Routes { get; set; } = [];

    static string GetNavigationRoutes => EnumHelper.GetEnumDescription(AppActions.GetNavigationRoutes);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Routes = await LoadData();
    }

    async Task<List<NavigationRouteVM>> LoadData()
    {
        var action = await AppActionFactory.RunAsync<IEnumerable<NavigationRouteVM>>(async () =>
        {
            if (Guid.TryParse(AppAuthenticationService.GetUserId(), out Guid userId))
            {

                var response = await NavigationRouteHandler.GetAllowedRoutesAsync(userId);
                return response;
            }
            else
            {
                var response = await NavigationRouteHandler.GetAllRoutesAsync();
                return response;
            }
        },
        AppActionOptionPresets.Loading(GetNavigationRoutes));

        return action.Result?.ToList() ?? [];
    }


    RenderFragment RenderMenu()
    {
        return builder =>
        {
            builder.OpenComponent<RadzenMenu>(0);
            builder.AddAttribute(1, "class", "rz-w-100");
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                Routes.ForEach(r =>
                {
                    childBuilder.AddContent(r.Position ?? 0, RenderMenu(r));
                });
            }));
            builder.CloseComponent();
        };
    }

    static RenderFragment RenderMenu(NavigationRouteVM route)
    {
        return builder =>
        {
            builder.OpenComponent<RadzenMenuItem>(0);
            if (route.Parent is null)
                builder.AddAttribute(1, "class", "rz-mx-1");
            builder.AddAttribute(2, "Text", route.Name);
            if (!string.IsNullOrEmpty(route.Icon))
                builder.AddAttribute(3, "Icon", route.Icon);
            if (!string.IsNullOrEmpty(route.Uri))
                builder.AddAttribute((string.IsNullOrEmpty(route.Icon) ? 3 : 4), "Path", route.Uri);

            if (route.Children.Count > 0)
                builder.AddAttribute((string.IsNullOrEmpty(route.Icon) ? (string.IsNullOrEmpty(route.Uri) ? 3 : 4) : 5), "ChildContent", (RenderFragment)(childBuilder =>
                {
                    List<NavigationRouteVM> childRoutes = [.. route.Children.OrderBy(x => x.Position)];
                    childRoutes.ForEach(cr =>
                    {
                        childBuilder.AddContent(0, RenderMenu(cr));
                    });
                }));

            builder.CloseComponent();
        };
    }
}
