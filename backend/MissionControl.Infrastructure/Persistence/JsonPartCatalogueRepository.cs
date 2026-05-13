using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Infrastructure.Persistence;

public class JsonPartCatalogueRepository : IPartCatalogueRepository
{
    private readonly string _filePath;
    private readonly string _seedFilePath;
    private IReadOnlyList<CataloguePart>? _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonPartCatalogueRepository(IOptions<JsonPartCatalogueStorageOptions> options)
    {
        _filePath = options.Value.FilePath;
        _seedFilePath = options.Value.SeedFilePath;
    }

    public async Task<IReadOnlyList<CataloguePart>> GetAllAsync()
    {
        return _cache ??= await LoadAsync();
    }

    public async Task<CataloguePart?> GetByIdAsync(string id)
    {
        var parts = await GetAllAsync();
        return parts.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<CataloguePart>> GetByCategoryAsync(PartCategory category)
    {
        var parts = await GetAllAsync();
        return parts.Where(p => p.Category == category).ToList();
    }

    public async Task<IReadOnlyList<CataloguePart>> SearchByNameAsync(string query)
    {
        var parts = await GetAllAsync();
        return parts
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || p.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<IReadOnlyList<CataloguePart>> LoadAsync()
    {
        // Fall back to seed file if the primary data file is absent (e.g. fresh volume mount)
        var resolvedPath = File.Exists(_filePath) ? _filePath
                         : File.Exists(_seedFilePath) ? _seedFilePath
                         : null;

        if (resolvedPath is null)
            return Array.Empty<CataloguePart>();

        var json = await File.ReadAllTextAsync(resolvedPath);
        var records = JsonSerializer.Deserialize<List<PartRecord>>(json, JsonOptions)
                      ?? new List<PartRecord>();

        return records.Select(r => new CataloguePart
        {
            Id = r.Id,
            Name = r.Name,
            Category = r.Category,
            DryMass = r.DryMass,
            WetMass = r.WetMass,
            FuelCapacity = r.FuelCapacity?.Count > 0
                ? r.FuelCapacity
                : null,
            EngineStats = r.EngineStats == null ? null : new EngineStats
            {
                ThrustSeaLevel = r.EngineStats.ThrustSeaLevel,
                ThrustVacuum = r.EngineStats.ThrustVacuum,
                IspSeaLevel = r.EngineStats.IspSeaLevel,
                IspVacuum = r.EngineStats.IspVacuum,
                FuelTypes = r.EngineStats.FuelTypes
            }
        }).ToList();
    }

    private class PartRecord
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public PartCategory Category { get; set; }
        public double DryMass { get; set; }
        public double WetMass { get; set; }
        public Dictionary<FuelType, double>? FuelCapacity { get; set; }
        public EngineStatsRecord? EngineStats { get; set; }
    }

    private class EngineStatsRecord
    {
        public double ThrustSeaLevel { get; set; }
        public double ThrustVacuum { get; set; }
        public double IspSeaLevel { get; set; }
        public double IspVacuum { get; set; }
        public FuelType[] FuelTypes { get; set; } = Array.Empty<FuelType>();
    }
}
