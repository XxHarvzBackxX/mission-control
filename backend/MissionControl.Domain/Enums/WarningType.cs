namespace MissionControl.Domain.Enums;

public enum WarningType
{
    InsufficientDeltaV,
    LowReserveMargin,
    MissingRequiredField,
    MissingCrew,
    InvalidTimeRange,
    AdvisoryEndTimeWithoutStart
}
