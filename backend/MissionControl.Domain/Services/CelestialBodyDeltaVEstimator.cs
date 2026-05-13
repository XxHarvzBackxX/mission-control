using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Domain.Services;

/// <summary>
/// Estimates required delta-v between two celestial bodies using the vis-viva equation
/// and standard manoeuvre budgets. This is a pure static service with no side-effects.
/// All altitude values are in metres above body surface.
/// </summary>
public static class CelestialBodyDeltaVEstimator
{
    // Standard Kerbin delta-v budget reference values (m/s)
    private static readonly IReadOnlyDictionary<string, (double Ascent, double Transfer, double Insertion)> KnownTransfers
        = new Dictionary<string, (double, double, double)>(StringComparer.OrdinalIgnoreCase)
    {
        // format: launchBodyId_targetBodyId → (ascent dv, transfer dv, insertion dv)
        // These represent canonical KSP community delta-v map values
    };

    public static RequiredDeltaVResult Estimate(
        CelestialBody launchBody,
        CelestialBody targetBody,
        MissionCalculationProfile profile)
    {
        var isCustom = launchBody.IsCustom || targetBody.IsCustom;
        if (isCustom)
        {
            return EstimateWithFormula(launchBody, targetBody, profile, isApproximated: true,
                method: "Hohmann approximation (custom body)");
        }

        // Use formula-based Hohmann transfer approximation
        return EstimateWithFormula(launchBody, targetBody, profile, isApproximated: true,
            method: "Hohmann approximation");
    }

    private static RequiredDeltaVResult EstimateWithFormula(
        CelestialBody launchBody,
        CelestialBody targetBody,
        MissionCalculationProfile profile,
        bool isApproximated,
        string method)
    {
        if (profile.RequiredDeltaVOverride.HasValue)
        {
            return new RequiredDeltaVResult(
                profile.RequiredDeltaVOverride.Value,
                AscentDeltaV: 0,
                TransferDeltaV: 0,
                DescentDeltaV: 0,
                ReturnDeltaV: 0,
                EstimationMethod: "Manual override",
                IsApproximated: false);
        }

        double ascentDv = EstimateAscentDeltaV(launchBody, profile.TargetOrbitAltitude);
        double transferDv = EstimateTransferDeltaV(launchBody, targetBody, profile);
        double descentDv = 0;
        double returnDv = 0;

        if (profile.ProfileType == MissionProfileType.SurfaceLanding || profile.ProfileType == MissionProfileType.FullReturn)
        {
            descentDv = EstimateDescentDeltaV(targetBody, profile.TargetOrbitAltitude);
        }
        if (profile.ProfileType == MissionProfileType.FullReturn)
        {
            returnDv = EstimateReturnDeltaV(launchBody, targetBody, profile);
        }

        double total = ascentDv + transferDv + descentDv + returnDv;

        return new RequiredDeltaVResult(total, ascentDv, transferDv, descentDv, returnDv,
            method, isApproximated);
    }

    /// <summary>
    /// Estimates ascent delta-v to circular orbit at the given altitude.
    /// Uses: ΔV ≈ √(μ / (R + h)) − √(μ / (2R+h_atm+h)) + gravity_drag
    /// Simplified: ΔV_ascent ≈ orbital_speed × correction_factor
    /// </summary>
    private static double EstimateAscentDeltaV(CelestialBody body, double orbitAltitude)
    {
        double mu = body.SurfaceGravity * body.EquatorialRadius * body.EquatorialRadius;
        double r_orbit = body.EquatorialRadius + orbitAltitude;
        double orbitalSpeed = Math.Sqrt(mu / r_orbit);

        // Gravity and drag losses (approximate, atmosphere-aware)
        double gravDragLoss = body.HasAtmosphere
            ? orbitalSpeed * 0.15   // ~15% loss in atmosphere
            : orbitalSpeed * 0.05;  // ~5% gravity drag in vacuum

        return orbitalSpeed + gravDragLoss;
    }

    /// <summary>
    /// Estimates Hohmann transfer delta-v between two bodies.
    /// ΔV_transfer = ΔV_departure + ΔV_insertion
    /// </summary>
    private static double EstimateTransferDeltaV(
        CelestialBody launchBody,
        CelestialBody targetBody,
        MissionCalculationProfile profile)
    {
        // If target is a moon of launch body or vice versa, simplified transfer
        if (string.Equals(targetBody.ParentBodyId, launchBody.Id, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(launchBody.ParentBodyId, targetBody.Id, StringComparison.OrdinalIgnoreCase))
        {
            return EstimateMoonTransferDv(launchBody, targetBody, profile);
        }

        // Interplanetary transfer (rough Hohmann)
        return EstimateInterplanetaryTransferDv(launchBody, targetBody, profile);
    }

    private static double EstimateMoonTransferDv(
        CelestialBody from,
        CelestialBody to,
        MissionCalculationProfile profile)
    {
        // Departure burn from orbit + insertion into moon orbit
        if (from.SemiMajorAxis == null && to.SemiMajorAxis == null)
            return 500; // fallback estimate

        double muParent = to.ParentBodyId != null
            ? EstimateParentMu(to)
            : EstimateParentMu(from);

        double r1 = (from.SemiMajorAxis ?? from.EquatorialRadius + profile.TargetOrbitAltitude);
        double r2 = (to.SemiMajorAxis ?? to.EquatorialRadius + profile.TargetOrbitAltitude);

        // Hohmann transfer semi-major axis
        double a_transfer = (r1 + r2) / 2;

        double v_circular_r1 = Math.Sqrt(muParent / r1);
        double v_transfer_r1 = Math.Sqrt(muParent * (2.0 / r1 - 1.0 / a_transfer));
        double dv_departure = Math.Abs(v_transfer_r1 - v_circular_r1);

        double v_circular_r2 = Math.Sqrt(muParent / r2);
        double v_transfer_r2 = Math.Sqrt(muParent * (2.0 / r2 - 1.0 / a_transfer));
        double dv_insertion = Math.Abs(v_circular_r2 - v_transfer_r2);

        return dv_departure + dv_insertion;
    }

    private static double EstimateInterplanetaryTransferDv(
        CelestialBody from,
        CelestialBody to,
        MissionCalculationProfile profile)
    {
        if (from.SemiMajorAxis == null || to.SemiMajorAxis == null)
            return 1000; // fallback estimate

        // Use Kerbol as parent, approximate mu from Kerbin reference orbit
        double muKerbol = 3.5316e12; // Kerbol gravitational parameter (m³/s²)

        double r1 = from.SemiMajorAxis.Value;
        double r2 = to.SemiMajorAxis.Value;
        double a_transfer = (r1 + r2) / 2;

        double v1 = Math.Sqrt(muKerbol / r1);
        double v_transfer_1 = Math.Sqrt(muKerbol * (2.0 / r1 - 1.0 / a_transfer));
        double dv1 = Math.Abs(v_transfer_1 - v1);

        double v2 = Math.Sqrt(muKerbol / r2);
        double v_transfer_2 = Math.Sqrt(muKerbol * (2.0 / r2 - 1.0 / a_transfer));
        double dv2 = Math.Abs(v_transfer_2 - v2);

        return dv1 + dv2;
    }

    private static double EstimateDescentDeltaV(CelestialBody targetBody, double orbitAltitude)
    {
        // For bodies without atmosphere: full landing burn required
        // For bodies with atmosphere: aerobraking handles most of it
        if (targetBody.HasAtmosphere)
            return EstimateAscentDeltaV(targetBody, orbitAltitude) * 0.2; // aerobraking savings
        return EstimateAscentDeltaV(targetBody, orbitAltitude); // full powered landing
    }

    private static double EstimateReturnDeltaV(
        CelestialBody launchBody,
        CelestialBody targetBody,
        MissionCalculationProfile profile)
    {
        // Rough estimate: return ≈ forward journey transfer + ascent from target
        return EstimateAscentDeltaV(targetBody, profile.TargetOrbitAltitude)
             + EstimateTransferDeltaV(targetBody, launchBody, profile);
    }

    private static double EstimateParentMu(CelestialBody body)
    {
        // Approximate GM from orbital parameters: μ = v² × r for circular orbit
        // Use default orbit as reference
        if (body.SemiMajorAxis == null) return 3.5e12;
        double r = body.SemiMajorAxis.Value;
        // Kerbol mu is our primary reference
        return 3.5316e12; // Kerbol
    }
}
