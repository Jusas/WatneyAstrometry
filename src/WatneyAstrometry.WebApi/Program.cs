// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.


// https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60?view=aspnetcore-6.0&tabs=visual-studio

using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using WatneyAstrometry.WebApi;
using WatneyAstrometry.WebApi.Authentication;
using WatneyAstrometry.WebApi.Controllers;
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

builder.Services.AddControllers().ConfigureApplicationPartManager(manager =>
{
    manager.FeatureProviders.Remove(manager.FeatureProviders.OfType<ControllerFeatureProvider>().FirstOrDefault());
    manager.FeatureProviders.Add(new ControllerProvider(apiConfig));
});

builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ApiVersionReader = new UrlSegmentApiVersionReader();
    config.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(config =>
{
    config.GroupNameFormat = "'v'VVV";
    config.SubstituteApiVersionInUrl = true;
});


if (apiConfig.EnableSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(setup =>
    {
        var versionProvider = builder.Services.BuildServiceProvider()
            .GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in versionProvider.ApiVersionDescriptions)
        {
            setup.SwaggerDoc(description.GroupName, new OpenApiInfo()
            {
                Title = "Watney API",
                Version = description.ApiVersion.ToString()
            });
        }
        
        var xmlApiDocFileName = Path.Combine(executableDir, "apidoc.xml");
        if(File.Exists(xmlApiDocFileName))
            setup.IncludeXmlComments(xmlApiDocFileName, true);

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
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-6.0
    config.Limits.MaxRequestBodySize = apiConfig.MaxImageSizeBytes;
    //config.ConfigureEndpointDefaults(lo => lo.);
});

builder.Services.AddAutoMapper(typeof(ServiceRegistration).Assembly);
builder.Services.AddSolverApiServices(builder.Configuration, apiConfig);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (apiConfig.EnableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        var versionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in versionProvider.ApiVersionDescriptions)
        {
            config.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

if(!string.IsNullOrEmpty(apiConfig.Authentication))
    app.MapControllers();
else
    app.MapControllers().WithMetadata(new AllowAnonymousAttribute());



ServiceRegistration.InitializeApiServices(app.Services);

app.Run();
