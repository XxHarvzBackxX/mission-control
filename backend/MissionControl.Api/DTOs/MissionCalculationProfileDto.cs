namespace MissionControl.Api.DTOs;

public class MissionCalculationProfileDto
{
    public string LaunchBodyId { get; set; } = null!;
    public string TargetBodyId { get; set; } = null!;
    public string ProfileType { get; set; } = null!;
    public double TargetOrbitAltitude { get; set; }
    public double AtmosphericEfficiencyMultiplier { get; set; } = 0.85;
    public double SafetyMarginPercent { get; set; } = 10.0;
    public double? RequiredDeltaVOverride { get; set; }
}

public class RequiredDeltaVBreakdownDto
{
    public double TotalRequiredDeltaV { get; set; }
    public double AscentDeltaV { get; set; }
    public double TransferDeltaV { get; set; }
    public double DescentDeltaV { get; set; }
    public double ReturnDeltaV { get; set; }
    public string EstimationMethod { get; set; } = null!;
    public bool IsApproximated { get; set; }
}
