using MissionControl.Domain.Enums;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Domain.Services;

public static class ReadinessCalculator
{
    /// <summary>
    /// Evaluates mission readiness based on delta-v margins and crew requirements.
    /// Returns a <see cref="ReadinessResult"/> containing the overall state and accumulated warnings.
    /// </summary>
    public static ReadinessResult Calculate(
        double availableDv,
        double requiredDv,
        MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers)
    {
        var warnings = new List<Warning>();
        var reserveMarginPercent = (availableDv - requiredDv) / requiredDv * 100.0;

        if (availableDv < requiredDv)
        {
            warnings.Add(new Warning(
                WarningType.InsufficientDeltaV,
                $"Available delta-v ({availableDv} m/s) is less than required ({requiredDv} m/s).",
                IsBlocking: true));
        }

        if (reserveMarginPercent < 10.0)
        {
            warnings.Add(new Warning(
                WarningType.LowReserveMargin,
                $"Reserve margin is {reserveMarginPercent:F1}% — below the 10% safety threshold.",
                IsBlocking: false));
        }

        if (controlMode == MissionControlMode.Crewed && crewMembers.Count == 0)
        {
            warnings.Add(new Warning(
                WarningType.MissingCrew,
                "At least one crew member is required for a Crewed mission.",
                IsBlocking: true));
        }

        var hasBlockingWarning = warnings.Any(w => w.IsBlocking);
        var hasLowMargin = warnings.Any(w => w.Type == WarningType.LowReserveMargin);

        ReadinessState state;
        if (hasBlockingWarning)
            state = ReadinessState.NotReady;
        else if (hasLowMargin)
            state = ReadinessState.AtRisk;
        else
            state = ReadinessState.Ready;

        return new ReadinessResult(state, warnings, Math.Round(reserveMarginPercent, 2));
    }
}

public sealed record ReadinessResult(
    ReadinessState State,
    IReadOnlyList<Warning> Warnings,
    double ReserveMarginPercent);
