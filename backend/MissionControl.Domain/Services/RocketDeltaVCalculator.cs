using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Domain.Services;

/// <summary>
/// Orchestrates per-stage delta-v calculation across all rocket stages in firing order
/// (highest stage number first), accumulating mass correctly as stages are jettisoned.
/// This is a pure static service with no side-effects.
/// </summary>
public static class RocketDeltaVCalculator
{
    public static RocketDeltaVResult Calculate(
        Rocket rocket,
        IReadOnlyList<CataloguePart> catalogueParts,
        CelestialBody launchBody,
        double? efficiencyFactorOverride = null)
    {
        var warnings = new List<Warning>();
        var stageResults = new List<StageDeltaVResult>();

        bool useVacuumIsp = !launchBody.HasAtmosphere;
        double efficiencyFactor = efficiencyFactorOverride
            ?? (launchBody.HasAtmosphere ? 0.85 : 1.0);
        double asparagusBonus = (rocket.UsesAsparagusStaging && launchBody.HasAtmosphere)
            ? rocket.AsparagusEfficiencyBonus
            : 0.0;

        // Check for command part
        var partLookup = catalogueParts.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        bool hasCommandPart = rocket.Stages
            .SelectMany(s => s.Parts)
            .Any(entry =>
            {
                if (!partLookup.TryGetValue(entry.PartId, out var p)) return false;
                return p.Category == PartCategory.Pods || p.Category == PartCategory.CommandAndControl;
            });

        if (!hasCommandPart)
        {
            warnings.Add(new Warning(WarningType.NoCommandPart,
                "Rocket has no command pod or probe core.", IsBlocking: false));
        }

        warnings.Add(new Warning(WarningType.UnstableCraftAssumption,
            "Centre-of-mass and centre-of-drag stability are assumed, not calculated.", IsBlocking: false));

        // Compute total wet mass (all stages combined)
        double totalWetMass = rocket.Stages
            .SelectMany(s => s.Parts)
            .Sum(entry =>
            {
                if (!partLookup.TryGetValue(entry.PartId, out var p)) return 0;
                return p.WetMass * entry.Quantity;
            });

        double accumulatedMass = totalWetMass;

        // Iterate from highest-numbered stage down to 1 (KSP convention: highest = first to fire)
        var orderedStages = rocket.Stages.OrderByDescending(s => s.StageNumber).ToList();

        foreach (var stage in orderedStages)
        {
            var stageResult = StageDeltaVCalculator.Calculate(
                stage, catalogueParts, accumulatedMass, useVacuumIsp, efficiencyFactor, asparagusBonus);

            stageResults.Add(stageResult);

            // After burn: move to dry mass at end of this stage
            double stageFuelMass = stageResult.WetMass - stageResult.DryMass;
            accumulatedMass -= stageFuelMass;

            // If jettisoned: also remove this stage's hardware dry mass
            if (stage.IsJettisoned)
            {
                double stageHardwareDryMass = stage.Parts.Sum(entry =>
                {
                    if (!partLookup.TryGetValue(entry.PartId, out var p)) return 0;
                    return p.DryMass * entry.Quantity;
                });
                accumulatedMass -= stageHardwareDryMass;
            }
        }

        var allStageWarnings = stageResults.SelectMany(s => s.Warnings).ToList();
        var hasBlockingWarning = allStageWarnings.Any(w => w.IsBlocking) ||
                                 warnings.Any(w => w.IsBlocking);

        double totalDv = hasBlockingWarning
            ? 0
            : stageResults.Sum(s => s.EffectiveDeltaV);

        // Return stages in payload-first order (Stage 1 first)
        var orderedResults = stageResults.OrderBy(s => s.StageNumber).ToList();

        return new RocketDeltaVResult(
            totalDv,
            orderedResults,
            warnings,
            IsValid: !hasBlockingWarning);
    }
}
