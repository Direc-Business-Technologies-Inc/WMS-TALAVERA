using Application.UseCases;
using Database.Libraries;
using Database.MsSql;
using Database.MsSql.Core;
using Integration.SAP;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Shared.Services;
using Web.BlazorServer.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers;
using Web.BlazorServer.Registers;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();

// Add services to the container.
#region Infrastructure Configuration
builder.Services.AddDatabaseMsSqlServices();
builder.Services.AddDatabaseLibrariesServices();
builder.Services.AddSAPServicesIntegraton();
#endregion Infrastructure Configuration

#region Utilities Configuration
builder.Services.AddSharedServices();
#endregion Utilities Configuration

#region Application Configuration
builder.Services.AddAppUseCases();
#endregion Application Configuration

#region Radzen Configuration
builder.Services.AddRadzenComponents();
#endregion Radzen Configuration

#region BlazorServerRegisters Configuration
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddBlazorServerRegisters();
builder.Services.AddBlazorServerHandlers();
#endregion BlazorServerRegisters Configuration

#region Security
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/";
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AppAuthenticationService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AppPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, AuthorizationController>();
builder.Services.AddScoped<AppAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AppAuthenticationStateProvider>());
#endregion Security

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    InitializeAppDb.InitializeAppDbAsync(scope.ServiceProvider)
        .GetAwaiter();
}

app.Run();
