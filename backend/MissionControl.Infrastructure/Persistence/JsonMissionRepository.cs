using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MissionControl.Domain;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Interfaces;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Infrastructure.Persistence;

public class JsonMissionRepository : IMissionRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonMissionRepository(IOptions<JsonStorageOptions> options)
    {
        _filePath = options.Value.FilePath;
    }

    public async Task<IReadOnlyList<Mission>> GetAllAsync()
    {
        var records = await ReadFileAsync();
        return records.Select(Reconstitute).ToList();
    }

    public async Task<Mission?> GetByIdAsync(Guid id)
    {
        var records = await ReadFileAsync();
        var record = records.FirstOrDefault(r => r.Id == id);
        return record == null ? null : Reconstitute(record);
    }

    public async Task<Mission?> GetByNameAsync(string name)
    {
        var records = await ReadFileAsync();
        var record = records.FirstOrDefault(r =>
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        return record == null ? null : Reconstitute(record);
    }

    public async Task AddAsync(Mission mission)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadFileAsync();
            records.Add(ToRecord(mission));
            await WriteFileAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(Mission mission)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadFileAsync();
            var index = records.FindIndex(r => r.Id == mission.Id);
            if (index < 0)
                throw new DomainException($"Mission '{mission.Id}' not found.");
            records[index] = ToRecord(mission);
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
                throw new DomainException($"Mission '{id}' not found.");
            await WriteFileAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<MissionRecord>> ReadFileAsync()
    {
        if (!File.Exists(_filePath))
            return new List<MissionRecord>();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<MissionRecord>>(json, JsonOptions)
               ?? new List<MissionRecord>();
    }

    private async Task WriteFileAsync(List<MissionRecord> records)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(records, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private static Mission Reconstitute(MissionRecord r)
    {
        return Mission.Reconstitute(
            r.Id,
            r.Name,
            new KspBodyValue(r.TargetBody.Value, r.TargetBody.IsCustom),
            new KspBodyValue(r.MissionType.Value, r.MissionType.IsCustom),
            r.AvailableDeltaV,
            r.RequiredDeltaV,
            r.ControlMode,
            r.CrewMembers,
            r.ProbeCore != null ? new KspBodyValue(r.ProbeCore.Value, r.ProbeCore.IsCustom) : null,
            r.StartMissionTime.HasValue ? new KerbinTime(r.StartMissionTime.Value) : null,
            r.EndMissionTime.HasValue ? new KerbinTime(r.EndMissionTime.Value) : null);
    }

    private static MissionRecord ToRecord(Mission m)
    {
        return new MissionRecord
        {
            Id = m.Id,
            Name = m.Name,
            TargetBody = new KspBodyRecord { Value = m.TargetBody.Value, IsCustom = m.TargetBody.IsCustom },
            MissionType = new KspBodyRecord { Value = m.MissionType.Value, IsCustom = m.MissionType.IsCustom },
            AvailableDeltaV = m.AvailableDeltaV,
            RequiredDeltaV = m.RequiredDeltaV,
            ControlMode = m.ControlMode,
            CrewMembers = m.CrewMembers.ToArray(),
            ProbeCore = m.ProbeCore != null
                ? new KspBodyRecord { Value = m.ProbeCore.Value, IsCustom = m.ProbeCore.IsCustom }
                : null,
            StartMissionTime = m.StartMissionTime?.TotalSeconds,
            EndMissionTime = m.EndMissionTime?.TotalSeconds
        };
    }

    private class MissionRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public KspBodyRecord TargetBody { get; set; } = null!;
        public KspBodyRecord MissionType { get; set; } = null!;
        public double AvailableDeltaV { get; set; }
        public double RequiredDeltaV { get; set; }
        public MissionControlMode ControlMode { get; set; }
        public string[] CrewMembers { get; set; } = Array.Empty<string>();
        public KspBodyRecord? ProbeCore { get; set; }
        public long? StartMissionTime { get; set; }
        public long? EndMissionTime { get; set; }
    }

    private class KspBodyRecord
    {
        public string Value { get; set; } = null!;
        public bool IsCustom { get; set; }
    }
}
