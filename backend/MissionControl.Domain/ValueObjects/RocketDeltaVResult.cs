namespace MissionControl.Domain.ValueObjects;

public sealed record RocketDeltaVResult(
    double TotalEffectiveDeltaV,
    IReadOnlyList<StageDeltaVResult> Stages,
    IReadOnlyList<Warning> Warnings,
    bool IsValid);
