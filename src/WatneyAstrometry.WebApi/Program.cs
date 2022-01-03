
// https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60?view=aspnetcore-6.0&tabs=visual-studio

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using WatneyAstrometry.WebApi;
using WatneyAstrometry.WebApi.Authentication;
using WatneyAstrometry.WebApi.Services;

var cultureInfo = CultureInfo.InvariantCulture;

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var builder = WebApplication.CreateBuilder(args);

// Load the configuration file.
var executableDir =
    Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
var configFile = Path.Combine(executableDir, "config.yml");

var apiConfig = WatneyApiConfiguration.Load(configFile);
builder.Services.AddSingleton<WatneyApiConfiguration>(apiConfig);

// Add services to the container.


if (!string.IsNullOrEmpty(apiConfig.Authentication))
{
    builder.Services.AddAuthentication(authOpts =>
    {
        authOpts.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
        authOpts.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
    }).AddApiKeySupport(opts => { });
}

builder.Services.AddControllers();


if (apiConfig.EnableSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(setup =>
    {
        if ("apikey".Equals(apiConfig.Authentication))
        {
            var securityScheme = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "apikey",
                Description = "Usage requires API Key in the 'apikey' header"
            };

            setup.AddSecurityDefinition("apikey", securityScheme);
            var keyScheme = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "apikey",
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "apikey"
                }
            };
            setup.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                { keyScheme, new List<string>() }
            });
        }
    });
}


builder.WebHost.ConfigureKestrel(config =>
{
    config.Limits.MaxRequestBodySize = apiConfig.MaxImageSizeBytes;
});


builder.Services.AddSolverApiServices(builder.Configuration, apiConfig);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (apiConfig.EnableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

if(!string.IsNullOrEmpty(apiConfig.Authentication))
    app.MapControllers();
else
    app.MapControllers().WithMetadata(new AllowAnonymousAttribute());






ServiceRegistration.InitializeApiServices(app.Services);

app.Run();
