using Application.UseCases.Queries.System;
using Dapper;
using Mapster;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Implementations.System;

public class NavigationRouteHandler(
    ISender Sender)
    : INavigationRouteHandler
{
    public async Task<IEnumerable<NavigationRouteVM>> GetAllowedRoutesAsync(Guid userId)
    {
        GetAllowedRoutesQry qry = new(userId);
        var dto = await Sender.Send(qry);

        IEnumerable<NavigationRouteVM> vm = dto.Adapt<IEnumerable<NavigationRouteVM>>();
        foreach (var item in vm)
        {
            if (dto.First(v => v.Id == item.Id).ParentId is null)
                continue;

            item.Parent = vm.FirstOrDefault(i => i.Id == dto.First(v => v.Id == item.Id).ParentId);
        }

        HashSet<Guid> ToRemove = [];

        foreach (var item in vm)
        {
            if (item.Parent is null)
                continue;
            item.Parent.Children.Add(item);
            ToRemove.Add(item.Id);
        }

        vm = [.. vm.Where(item => !ToRemove.Contains(item.Id))
                   .OrderBy(item => item.Position)];

        return vm;
    }

    public async Task<IEnumerable<NavigationRouteVM>> GetAllRoutesAsync()
    {
        GetNavigationRoutesQry qry = new();
        var dto = await Sender.Send(qry);

        IEnumerable<NavigationRouteVM> vm = dto.Adapt<IEnumerable<NavigationRouteVM>>();
        foreach (var item in vm)
        {
            if (dto.First(v => v.Id == item.Id).ParentId is null)
                continue;

            item.Parent = vm.FirstOrDefault(i => i.Id == dto.First(v => v.Id == item.Id).ParentId);
        }

        HashSet<Guid> ToRemove = [];

        foreach (var item in vm)
        {
            if (item.Parent is null)
                continue;
            item.Parent.Children.Add(item);
            ToRemove.Add(item.Id);
        }

        vm = [.. vm.Where(item => !ToRemove.Contains(item.Id))
                   .OrderBy(item => item.Position)];

        return vm;
    }

    public async Task<IEnumerable<NavigationRouteVM>> GetModuleNavigationRoutesAsync(string moduleCode)
    {
        GetModuleNavigationRoutesQry qry = new(moduleCode);
        var response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<NavigationRouteVM>>();
    }
}
