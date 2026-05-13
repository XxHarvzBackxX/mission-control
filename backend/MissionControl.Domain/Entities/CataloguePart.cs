using MissionControl.Domain.Enums;

namespace MissionControl.Domain.Entities;

public sealed class CataloguePart
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public PartCategory Category { get; init; }
    public double DryMass { get; init; }
    public double WetMass { get; init; }
    public IReadOnlyDictionary<FuelType, double>? FuelCapacity { get; init; }
    public EngineStats? EngineStats { get; init; }
}

public sealed class EngineStats
{
    public double ThrustSeaLevel { get; init; }
    public double ThrustVacuum { get; init; }
    public double IspSeaLevel { get; init; }
    public double IspVacuum { get; init; }
    public IReadOnlyList<FuelType> FuelTypes { get; init; } = Array.Empty<FuelType>();
}
