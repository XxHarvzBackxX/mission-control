namespace MissionControl.Domain.Entities;

public sealed class CelestialBody
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? ParentBodyId { get; init; }
    public double EquatorialRadius { get; init; }
    public double SurfaceGravity { get; init; }
    public double SurfacePressure { get; init; }
    public double AtmosphereHeight { get; init; }
    public double? SphereOfInfluence { get; init; }
    public double? SemiMajorAxis { get; init; }
    public double DefaultOrbitAltitude { get; init; }
    public bool IsCustom { get; init; }

    public bool HasAtmosphere => SurfacePressure > 0;
}
