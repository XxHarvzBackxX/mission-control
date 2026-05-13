using MissionControl.Domain.Enums;

namespace MissionControl.Api.DTOs;

public class EngineStatsDto
{
    public double ThrustSeaLevel { get; set; }
    public double ThrustVacuum { get; set; }
    public double IspSeaLevel { get; set; }
    public double IspVacuum { get; set; }
    public List<string> FuelTypes { get; set; } = new();
}

public class PartDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public double DryMass { get; set; }
    public double WetMass { get; set; }
    public Dictionary<string, double>? FuelCapacity { get; set; }
    public EngineStatsDto? EngineStats { get; set; }
}
