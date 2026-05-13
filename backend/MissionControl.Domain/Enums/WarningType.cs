namespace MissionControl.Domain.Enums;

public enum WarningType
{
    InsufficientDeltaV,
    LowReserveMargin,
    MissingRequiredField,
    MissingCrew,
    InvalidTimeRange,
    AdvisoryEndTimeWithoutStart,

    // Rocket and calculation warnings
    NoCommandPart,
    NoEngine,
    NoFuelSource,
    MixedFuelUncertainty,
    AtmosphericLossApplied,
    UnstableCraftAssumption,
    CustomBodyApproximation,
    ManualOverrideApplied,
    AsparagusApproximationApplied
}

