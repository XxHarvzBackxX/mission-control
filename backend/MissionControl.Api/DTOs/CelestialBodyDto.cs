namespace MissionControl.Api.DTOs;

public class CelestialBodyDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ParentBodyId { get; set; }
    public double EquatorialRadius { get; set; }
    public double SurfaceGravity { get; set; }
    public double SurfacePressure { get; set; }
    public double AtmosphereHeight { get; set; }
    public double? SphereOfInfluence { get; set; }
    public double? SemiMajorAxis { get; set; }
    public double DefaultOrbitAltitude { get; set; }
    public bool HasAtmosphere { get; set; }
    public bool IsCustom { get; set; }
}

public class CreateCustomBodyDto
{
    public string Name { get; set; } = null!;
    public string? ParentBodyId { get; set; }
    public double EquatorialRadius { get; set; }
    public double SurfaceGravity { get; set; }
    public double SurfacePressure { get; set; }
    public double AtmosphereHeight { get; set; }
    public double DefaultOrbitAltitude { get; set; }
}
