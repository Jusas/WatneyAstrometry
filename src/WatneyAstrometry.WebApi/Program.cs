
// https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60?view=aspnetcore-6.0&tabs=visual-studio

using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using WatneyAstrometry.WebApi;
using WatneyAstrometry.WebApi.Services;

var cultureInfo = CultureInfo.InvariantCulture;

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    // todo enums as strings?
});

builder.WebHost.ConfigureKestrel(config =>
{
    config.Limits.MaxRequestBodySize = 50_000_000;
});


builder.Services.AddSolverApiServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

ServiceRegistration.InitializeApiServices(app.Services);

app.Run();
