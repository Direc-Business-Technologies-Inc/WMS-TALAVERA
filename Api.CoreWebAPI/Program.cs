using Api.CoreWebAPI.Registers;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
// Add services to the container.

//builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.ConfigureServices();

builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    string key = Environment.GetEnvironmentVariable("WMSAPI_SIGN");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key))
    };
});

var app = builder.Build();

// Deploy verification without waiting for a field failure: the stamp is the git
// commit the assembly was COMPILED from (SDK AssemblyInformationalVersion). The
// TO item-fulfillment flags are logged with it so the deployed behaviour is
// readable from the first log line, not inferred from a save result.
app.Logger.LogInformation(
    "API build {BuildStamp} | TO IF reconciliation={ReconciliationEnabled} failClosed={FailClosedRevalidationEnabled} restlet={UseRestlet} transformVerify={TransformVerify} maxTransformLines={MaxTransformLines}",
    Integration.NS.Services.NetSuiteApiClientService.BuildStampValue,
    Integration.NS.Services.NetSuiteApiClientService.ReconciliationEnabled,
    Integration.NS.Services.NetSuiteApiClientService.FailClosedRevalidationEnabled,
    Integration.NS.Services.NetSuiteApiClientService.UseRestletForTOFulfillment,
    Integration.NS.Services.NetSuiteApiClientService.TransformVerificationEnabled,
    Integration.NS.Services.NetSuiteApiClientService.MaxTransformLines);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.AppServices();