namespace MissionControl.Domain.ValueObjects;

public sealed record RequiredDeltaVResult(
    double TotalRequiredDeltaV,
    double AscentDeltaV,
    double TransferDeltaV,
    double DescentDeltaV,
    double ReturnDeltaV,
    string EstimationMethod,
    bool IsApproximated);
