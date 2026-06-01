using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations;

public static class NSImplementationDI
{
    public static IServiceCollection AddNSImplementationsIntegraton(this IServiceCollection services)
    {

        return services;
    }
}
