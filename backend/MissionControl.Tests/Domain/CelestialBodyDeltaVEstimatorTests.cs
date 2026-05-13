using MissionControl.Domain.Entities;
using MissionControl.Domain.Services;
using MissionControl.Domain.ValueObjects;
using MissionControl.Domain.Enums;

namespace MissionControl.Tests.Domain;

[TestFixture]
public class CelestialBodyDeltaVEstimatorTests
{
    private static CelestialBody Kerbin { get; } = new()
    {
        Id = "kerbin", Name = "Kerbin",
        EquatorialRadius = 600_000, SurfaceGravity = 9.81,
        SurfacePressure = 1.0, AtmosphereHeight = 70_000,
        SemiMajorAxis = 13_599_840_256, SphereOfInfluence = 84_159_286,
        DefaultOrbitAltitude = 80_000, IsCustom = false
    };

    private static CelestialBody Mun { get; } = new()
    {
        Id = "mun", Name = "Mun",
        ParentBodyId = "kerbin",
        EquatorialRadius = 200_000, SurfaceGravity = 1.63,
        SurfacePressure = 0, AtmosphereHeight = 0,
        SemiMajorAxis = 12_000_000,
        DefaultOrbitAltitude = 10_000, IsCustom = false
    };

    private static CelestialBody Minmus { get; } = new()
    {
        Id = "minmus", Name = "Minmus",
        ParentBodyId = "kerbin",
        EquatorialRadius = 60_000, SurfaceGravity = 0.491,
        SurfacePressure = 0, AtmosphereHeight = 0,
        SemiMajorAxis = 47_000_000,
        DefaultOrbitAltitude = 10_000, IsCustom = false
    };

    private static CelestialBody CustomBody { get; } = new()
    {
        Id = "custom-1", Name = "Custom World",
        EquatorialRadius = 400_000, SurfaceGravity = 5.0,
        SurfacePressure = 0, AtmosphereHeight = 0,
        IsCustom = true
    };

    private static MissionCalculationProfile DefaultProfile => MissionCalculationProfile.CreateDefault();

    [Test]
    public void Estimate_KerbinToMun_ReturnsPositiveValue()
    {
        var result = CelestialBodyDeltaVEstimator.Estimate(Kerbin, Mun, DefaultProfile);

        Assert.That(result.TotalRequiredDeltaV, Is.GreaterThan(0));
    }

    [Test]
    public void Estimate_OrbitInsertion_HasNoDescentOrReturn()
    {
        var profile = DefaultProfile with { ProfileType = MissionProfileType.OrbitInsertion };

        var result = CelestialBodyDeltaVEstimator.Estimate(Kerbin, Mun, profile);

        Assert.That(result.DescentDeltaV, Is.EqualTo(0));
        Assert.That(result.ReturnDeltaV, Is.EqualTo(0));
    }

    [Test]
    public void Estimate_FullReturn_HasAllComponents()
    {
        var profile = DefaultProfile with { ProfileType = MissionProfileType.FullReturn };

        var result = CelestialBodyDeltaVEstimator.Estimate(Kerbin, Mun, profile);

        Assert.That(result.AscentDeltaV, Is.GreaterThan(0));
        Assert.That(result.ReturnDeltaV, Is.GreaterThan(0));
    }

    [Test]
    public void Estimate_ManualOverride_ReturnsThatValueExactly()
    {
        const double overrideDv = 5000;
        var profile = DefaultProfile with { RequiredDeltaVOverride = overrideDv };

        var result = CelestialBodyDeltaVEstimator.Estimate(Kerbin, Mun, profile);

        Assert.That(result.TotalRequiredDeltaV, Is.EqualTo(overrideDv));
        Assert.That(result.IsApproximated, Is.False);
        Assert.That(result.EstimationMethod, Is.EqualTo("Manual override"));
    }

    [Test]
    public void Estimate_CustomBody_IsMarkedAsApproximated()
    {
        var result = CelestialBodyDeltaVEstimator.Estimate(Kerbin, CustomBody, DefaultProfile);

        Assert.That(result.IsApproximated, Is.True);
    }

    [Test]
    public void Estimate_MinmusLessThanMun_DueToDifferentGravity()
    {
        var profile = DefaultProfile with { ProfileType = MissionProfileType.FullReturn };

        var munResult = CelestialBodyDeltaVEstimator.Estimate(Kerbin, Mun, profile);
        var minmusResult = CelestialBodyDeltaVEstimator.Estimate(Kerbin, Minmus, profile);

        // Minmus has lower gravity so landing/return should be cheaper
        // (transfer may differ, but total should reflect lower landing cost)
        Assert.That(minmusResult.DescentDeltaV, Is.LessThan(munResult.DescentDeltaV));
    }
}
