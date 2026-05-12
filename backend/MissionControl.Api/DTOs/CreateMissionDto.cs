namespace MissionControl.Api.DTOs;

public class CreateMissionDto
{
    public string Name { get; set; } = null!;
    public string TargetBodyValue { get; set; } = null!;
    public bool TargetBodyIsCustom { get; set; }
    public string MissionTypeValue { get; set; } = null!;
    public bool MissionTypeIsCustom { get; set; }
    public double AvailableDeltaV { get; set; }
    public double RequiredDeltaV { get; set; }
    public string ControlMode { get; set; } = null!;
    public string[] CrewMembers { get; set; } = Array.Empty<string>();
    public string? ProbeCoreValue { get; set; }
    public bool ProbeCoreIsCustom { get; set; }
    public long? StartMissionTime { get; set; }
    public long? EndMissionTime { get; set; }
}

public class UpdateMissionDto : CreateMissionDto { }
