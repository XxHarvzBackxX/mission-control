using System.Text.Json.Serialization;
using MissionControl.Domain.Interfaces;
using MissionControl.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<JsonStorageOptions>(
    builder.Configuration.GetSection("JsonStorage"));
builder.Services.AddSingleton<IMissionRepository, JsonMissionRepository>();

builder.Services.Configure<JsonRocketStorageOptions>(
    builder.Configuration.GetSection("RocketStorage"));
builder.Services.AddSingleton<IRocketRepository, JsonRocketRepository>();

builder.Services.Configure<JsonPartCatalogueStorageOptions>(
    builder.Configuration.GetSection("PartCatalogueStorage"));
builder.Services.AddSingleton<IPartCatalogueRepository, JsonPartCatalogueRepository>();

builder.Services.Configure<JsonCelestialBodyStorageOptions>(
    builder.Configuration.GetSection("CelestialBodyStorage"));
builder.Services.AddSingleton<ICelestialBodyRepository, JsonCelestialBodyRepository>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
