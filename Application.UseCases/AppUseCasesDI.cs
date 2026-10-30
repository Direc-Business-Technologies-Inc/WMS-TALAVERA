using Application.UseCases.Behaviors;
using Mapster;
using MapsterMapper;
using MediatR;
using Application.UseCases.Commands.Transaction.StockTransferRequest;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UseCases;

public static class AppUseCasesDI
{
    public static IServiceCollection AddAppUseCases(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Default.PreserveReference(true);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddSingleton(StrAutoFulfillmentOptions.FromEnvironment());
        config.Scan(typeof(AppUseCasesDI).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = Environment.GetEnvironmentVariable("LuckyPennySoftwareLicenseKey");
            cfg.RegisterServicesFromAssemblies(typeof(AppUseCasesDI).Assembly);

            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionalDocumentBehavior<,>));
        });

        return services;
    }
}
