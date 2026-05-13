using MissionControl.Domain.Enums;

namespace MissionControl.Domain.ValueObjects;

public sealed record MissionCalculationProfile(
    string LaunchBodyId,
    string TargetBodyId,
    MissionProfileType ProfileType,
    double TargetOrbitAltitude,
    double AtmosphericEfficiencyMultiplier,
    double SafetyMarginPercent,
    double? RequiredDeltaVOverride)
{
    public static MissionCalculationProfile CreateDefault() => new(
        LaunchBodyId: "kerbin",
        TargetBodyId: "mun",
        ProfileType: MissionProfileType.OrbitInsertion,
        TargetOrbitAltitude: 80_000,
        AtmosphericEfficiencyMultiplier: 0.85,
        SafetyMarginPercent: 10.0,
        RequiredDeltaVOverride: null);
}
