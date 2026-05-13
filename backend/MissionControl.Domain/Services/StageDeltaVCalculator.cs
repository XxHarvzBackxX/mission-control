using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Domain.Services;

/// <summary>
/// Calculates delta-v for a single rocket stage using the Tsiolkovsky rocket equation:
///   ΔV = Isp × g₀ × ln(m_wet / m_dry) × efficiencyFactor × (1 + asparagusBonus)
/// where g₀ = 9.80665 m/s².
/// This is a pure static service with no side-effects.
/// </summary>
public static class StageDeltaVCalculator
{
    private const double G0 = 9.80665;

    public static StageDeltaVResult Calculate(
        Stage stage,
        IReadOnlyList<CataloguePart> catalogueParts,
        double wetMassAtBurnStart,
        bool useVacuumIsp,
        double efficiencyFactor,
        double asparagusBonus)
    {
        var warnings = new List<Warning>();
        var partLookup = catalogueParts.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        var engines = new List<CataloguePart>();
        var hasFuel = false;
        var fuelTypesPresent = new HashSet<FuelType>();
        double stageFuelMass = 0;

        foreach (var entry in stage.Parts)
        {
            if (!partLookup.TryGetValue(entry.PartId, out var part))
                continue;

            if (part.EngineStats != null)
            {
                for (var i = 0; i < entry.Quantity; i++)
                    engines.Add(part);
            }

            if (part.FuelCapacity != null)
            {
                hasFuel = true;
                foreach (var kv in part.FuelCapacity)
                    fuelTypesPresent.Add(kv.Key);

                stageFuelMass += (part.WetMass - part.DryMass) * entry.Quantity;
            }
        }

        if (engines.Count == 0)
        {
            warnings.Add(new Warning(WarningType.NoEngine,
                "This stage has no engine — delta-v cannot be calculated.", IsBlocking: true));
            return new StageDeltaVResult(stage.StageNumber, stage.Name,
                wetMassAtBurnStart, wetMassAtBurnStart, 0, 0, efficiencyFactor, asparagusBonus, 0, warnings);
        }

        if (!hasFuel)
        {
            warnings.Add(new Warning(WarningType.NoFuelSource,
                "This stage has engines but no fuel tanks.", IsBlocking: true));
            return new StageDeltaVResult(stage.StageNumber, stage.Name,
                wetMassAtBurnStart, wetMassAtBurnStart, 0, 0, efficiencyFactor, asparagusBonus, 0, warnings);
        }

        // Check fuel compatibility
        var engineFuelTypes = engines
            .SelectMany(e => e.EngineStats!.FuelTypes)
            .Distinct()
            .ToHashSet();

        var hasCompatibleFuel = engineFuelTypes.Overlaps(fuelTypesPresent);
        if (!hasCompatibleFuel)
        {
            warnings.Add(new Warning(WarningType.NoFuelSource,
                "Stage engines have no compatible fuel in this stage.", IsBlocking: true));
            return new StageDeltaVResult(stage.StageNumber, stage.Name,
                wetMassAtBurnStart, wetMassAtBurnStart, 0, 0, efficiencyFactor, asparagusBonus, 0, warnings);
        }

        if (engineFuelTypes.Count > 1 || fuelTypesPresent.Count > 1)
        {
            warnings.Add(new Warning(WarningType.MixedFuelUncertainty,
                "Stage has mixed fuel types; Isp calculation uses a thrust-weighted average.", IsBlocking: false));
        }

        var isp = ComputeEffectiveIsp(engines, useVacuumIsp);

        if (efficiencyFactor < 1.0)
        {
            warnings.Add(new Warning(WarningType.AtmosphericLossApplied,
                $"Atmospheric efficiency factor {efficiencyFactor:P0} applied.", IsBlocking: false));
        }

        if (asparagusBonus > 0)
        {
            warnings.Add(new Warning(WarningType.AsparagusApproximationApplied,
                $"Asparagus staging bonus {asparagusBonus:P0} applied to atmospheric ascent delta-v.", IsBlocking: false));
        }

        var dryMass = wetMassAtBurnStart - stageFuelMass;
        if (dryMass <= 0 || wetMassAtBurnStart <= dryMass)
        {
            warnings.Add(new Warning(WarningType.NoFuelSource,
                "Effective fuel mass is zero or negative; cannot calculate delta-v.", IsBlocking: true));
            return new StageDeltaVResult(stage.StageNumber, stage.Name,
                wetMassAtBurnStart, dryMass, isp, 0, efficiencyFactor, asparagusBonus, 0, warnings);
        }

        var rawDeltaV = isp * G0 * Math.Log(wetMassAtBurnStart / dryMass);
        var effectiveDeltaV = rawDeltaV * efficiencyFactor * (1.0 + asparagusBonus);

        return new StageDeltaVResult(
            stage.StageNumber,
            stage.Name,
            wetMassAtBurnStart,
            dryMass,
            isp,
            rawDeltaV,
            efficiencyFactor,
            asparagusBonus,
            effectiveDeltaV,
            warnings);
    }

    /// <summary>
    /// Computes thrust-weighted effective Isp across multiple engines:
    ///   Isp_eff = (Σ thrust_i) / (Σ thrust_i / Isp_i)
    /// </summary>
    private static double ComputeEffectiveIsp(List<CataloguePart> engines, bool useVacuumIsp)
    {
        double totalThrust = 0;
        double thrustOverIsp = 0;

        foreach (var engine in engines)
        {
            var stats = engine.EngineStats!;
            var thrust = useVacuumIsp ? stats.ThrustVacuum : stats.ThrustSeaLevel;
            var isp = useVacuumIsp ? stats.IspVacuum : stats.IspSeaLevel;

            if (thrust <= 0 || isp <= 0) continue;

            totalThrust += thrust;
            thrustOverIsp += thrust / isp;
        }

        if (thrustOverIsp <= 0) return 0;
        return totalThrust / thrustOverIsp;
    }
}
