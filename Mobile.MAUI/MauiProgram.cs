using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Mobile.MAUI.Repositories;
using Mobile.MAUI.Services;
using Radzen;

namespace Mobile.MAUI
{
    public static class MauiProgram
    {
        public static class BroadcastService
        {
            public static event EventHandler<string> BroadcastReceived;

            public static void OnBroadcastReceived(string message)
            {
                BroadcastReceived?.Invoke(null, message);
            }
        }
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, JWTAuthStateProvider>();
            builder.Services.AddScoped<JWTAuthStateProvider>();
            builder.Services.AddAuthorizationCore();
            #region Radzen related
            builder.Services.AddRadzenComponents();
            #endregion Radzen related

            #region Custom Services
            builder.Services.AddAppServices();
            builder.Services.AddAppRepositories();
            #endregion Custom Services

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
