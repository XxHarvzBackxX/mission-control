using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Infrastructure.Persistence;

public class JsonCelestialBodyRepository : ICelestialBodyRepository
{
    private readonly string _filePath;
    private readonly string _seedFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonCelestialBodyRepository(IOptions<JsonCelestialBodyStorageOptions> options)
    {
        _filePath = options.Value.FilePath;
        _seedFilePath = options.Value.SeedFilePath;
    }

    public async Task<IReadOnlyList<CelestialBody>> GetAllAsync()
    {
        var store = await ReadFileAsync();
        var all = store.StockBodies.Concat(store.CustomBodies).ToList();
        return all.Select(Reconstitute).ToList();
    }

    public async Task<CelestialBody?> GetByIdAsync(string id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddCustomAsync(CelestialBody body)
    {
        await _lock.WaitAsync();
        try
        {
            var store = await ReadFileAsync();
            var existing = store.CustomBodies
                .FirstOrDefault(b => string.Equals(b.Id, body.Id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                throw new InvalidOperationException($"A custom body with id '{body.Id}' already exists.");

            store.CustomBodies.Add(ToRecord(body));
            await WriteFileAsync(store);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<CelestialBodyStore> ReadFileAsync()
    {
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<CelestialBodyStore>(json, JsonOptions)
                   ?? new CelestialBodyStore();
        }

        // Primary file missing — initialise from seed so custom bodies can be persisted later
        if (File.Exists(_seedFilePath))
        {
            var seedJson = await File.ReadAllTextAsync(_seedFilePath);
            var store = JsonSerializer.Deserialize<CelestialBodyStore>(seedJson, JsonOptions)
                        ?? new CelestialBodyStore();
            // Write seed content to the runtime path so future AddCustomAsync writes work correctly
            await WriteFileAsync(store);
            return store;
        }

        return new CelestialBodyStore();
    }

    private async Task WriteFileAsync(CelestialBodyStore store)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(store, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private static CelestialBody Reconstitute(BodyRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        ParentBodyId = r.ParentBodyId,
        EquatorialRadius = r.Radius,
        SurfaceGravity = r.Gravity,
        SurfacePressure = r.Pressure,
        AtmosphereHeight = r.AtmHeight,
        SphereOfInfluence = r.Soi,
        SemiMajorAxis = r.Sma,
        DefaultOrbitAltitude = r.DefaultOrbit,
        IsCustom = r.IsCustom
    };

    private static BodyRecord ToRecord(CelestialBody b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        ParentBodyId = b.ParentBodyId,
        Radius = b.EquatorialRadius,
        Gravity = b.SurfaceGravity,
        Pressure = b.SurfacePressure,
        AtmHeight = b.AtmosphereHeight,
        Soi = b.SphereOfInfluence,
        Sma = b.SemiMajorAxis,
        DefaultOrbit = b.DefaultOrbitAltitude,
        IsCustom = b.IsCustom
    };

    private class CelestialBodyStore
    {
        [JsonPropertyName("stockBodies")]
        public List<BodyRecord> StockBodies { get; set; } = new();
        [JsonPropertyName("customBodies")]
        public List<BodyRecord> CustomBodies { get; set; } = new();
    }

    private class BodyRecord
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ParentBodyId { get; set; }
        public double Radius { get; set; }
        public double Gravity { get; set; }
        public double Pressure { get; set; }
        public double AtmHeight { get; set; }
        public double? Soi { get; set; }
        public double? Sma { get; set; }
        public double DefaultOrbit { get; set; }
        public bool IsCustom { get; set; }
    }
}
