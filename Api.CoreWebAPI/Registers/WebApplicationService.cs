using Application.UseCases;
using Database.Libraries;
using Database.MsSql;
using Integration.NS;
using Integration.SAP;
using Microsoft.OpenApi.Models;
using Shared.Services;
using System.Reflection;

namespace Api.CoreWebAPI.Registers;

public static class WebApplicationService
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        #region Infrastructure Configuration
        builder.Services.AddDatabaseMsSqlServices();
        builder.Services.AddDatabaseLibrariesServices();
        builder.Services.AddSAPServicesIntegraton();
        builder.Services.AddNSServicesIntegraton();
        #endregion Infrastructure Configuration

        #region Utilities Configuration
        builder.Services.AddSharedServices();
        #endregion Utilities Configuration

        #region Application Configuration
        builder.Services.AddAppUseCases();
        #endregion Application Configuration

        builder.Services.AddCoreWebApiServices();
        // Add services to the container.
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
        // Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1",
                new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Direc API",
                    Description = "Direc API Documentation",
                    Version = "v1"
                });
            //options.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
            //{
            //    Url = "http://166.108.201.180:8093"
            //});
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOi...\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            // This tells Swagger to use the "Bearer" security scheme globally
            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            var fileName = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            options.IncludeXmlComments(filePath);
        });
        //builder.Services
        //    .AddServices();
        //builder.Services
        //    .AddDataLibraries()
        //    .AddDataServices();


        //builder.Services.AddTransient<INetSuiteApiClientService, NetSuiteApiClientService>();
        //builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        //builder.Services.AddScoped<ITimeProvider, SystemTimeProvider>();

        //builder.Services.AddAppLogging();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        //    builder.Services.AddIdentity<UserLogins, IdentityRole>()
        //.AddEntityFrameworkStores<Context>()
        //.AddUserManager<UserManager<UserLogins>>()
        //.AddDefaultTokenProviders();
    }
}
