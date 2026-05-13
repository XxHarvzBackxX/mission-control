namespace MissionControl.Domain.ValueObjects;

public sealed record StageDeltaVResult(
    int StageNumber,
    string StageName,
    double WetMass,
    double DryMass,
    double IspUsed,
    double RawDeltaV,
    double EfficiencyFactor,
    double AsparagusBonus,
    double EffectiveDeltaV,
    IReadOnlyList<Warning> Warnings);
