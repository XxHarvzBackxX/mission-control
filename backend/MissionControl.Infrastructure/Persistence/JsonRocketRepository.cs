using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MissionControl.Domain;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Interfaces;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Infrastructure.Persistence;

public class JsonRocketRepository : IRocketRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonRocketRepository(IOptions<JsonRocketStorageOptions> options)
    {
        _filePath = options.Value.FilePath;
    }

    public async Task<IReadOnlyList<Rocket>> GetAllAsync()
    {
        var records = await ReadFileAsync();
        return records.Select(Reconstitute).ToList();
    }

    public async Task<Rocket?> GetByIdAsync(Guid id)
    {
        var records = await ReadFileAsync();
        var record = records.FirstOrDefault(r => r.Id == id);
        return record == null ? null : Reconstitute(record);
    }

    public async Task<Rocket?> GetByNameAsync(string name)
    {
        var records = await ReadFileAsync();
        var record = records.FirstOrDefault(r =>
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        return record == null ? null : Reconstitute(record);
    }

    public async Task AddAsync(Rocket rocket)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadFileAsync();
            records.Add(ToRecord(rocket));
            await WriteFileAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(Rocket rocket)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadFileAsync();
            var index = records.FindIndex(r => r.Id == rocket.Id);
            if (index < 0)
                throw new DomainException($"Rocket '{rocket.Id}' not found.");
            records[index] = ToRecord(rocket);
            await WriteFileAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadFileAsync();
            var removed = records.RemoveAll(r => r.Id == id);
            if (removed == 0)
                throw new DomainException($"Rocket '{id}' not found.");
            await WriteFileAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetMissionIdsAssignedToRocketAsync(Guid rocketId)
    {
        // NOTE: This is a cross-repository query. The API layer will coordinate with the
        // mission repository. This implementation returns empty — mission assignment
        // is tracked on the Mission entity side.
        return await Task.FromResult(Array.Empty<Guid>());
    }

    private async Task<List<RocketRecord>> ReadFileAsync()
    {
        if (!File.Exists(_filePath))
            return new List<RocketRecord>();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<RocketRecord>>(json, JsonOptions)
               ?? new List<RocketRecord>();
    }

    private async Task WriteFileAsync(List<RocketRecord> records)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(records, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private static Rocket Reconstitute(RocketRecord r)
    {
        var stages = r.Stages.Select(s => Stage.Reconstitute(
            s.Id,
            s.StageNumber,
            s.Name,
            s.Parts.Select(p => new StageEntry(p.PartId, p.Quantity)).ToList(),
            s.IsJettisoned,
            s.Notes)).ToList();

        return Rocket.Reconstitute(r.Id, r.Name, r.Description, stages,
            r.UsesAsparagusStaging, r.AsparagusEfficiencyBonus, r.Notes);
    }

    private static RocketRecord ToRecord(Rocket rocket) => new()
    {
        Id = rocket.Id,
        Name = rocket.Name,
        Description = rocket.Description,
        Notes = rocket.Notes,
        UsesAsparagusStaging = rocket.UsesAsparagusStaging,
        AsparagusEfficiencyBonus = rocket.AsparagusEfficiencyBonus,
        Stages = rocket.Stages.Select(s => new StageRecord
        {
            Id = s.Id,
            StageNumber = s.StageNumber,
            Name = s.Name,
            IsJettisoned = s.IsJettisoned,
            Notes = s.Notes,
            Parts = s.Parts.Select(p => new StageEntryRecord
            {
                PartId = p.PartId,
                Quantity = p.Quantity
            }).ToList()
        }).ToList()
    };

    private class RocketRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Notes { get; set; }
        public bool UsesAsparagusStaging { get; set; }
        public double AsparagusEfficiencyBonus { get; set; }
        public List<StageRecord> Stages { get; set; } = new();
    }

    private class StageRecord
    {
        public Guid Id { get; set; }
        public int StageNumber { get; set; }
        public string Name { get; set; } = null!;
        public bool IsJettisoned { get; set; }
        public string? Notes { get; set; }
        public List<StageEntryRecord> Parts { get; set; } = new();
    }

    private class StageEntryRecord
    {
        public string PartId { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
