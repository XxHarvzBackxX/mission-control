namespace MissionControl.Api.DTOs;

public class MissionListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ReadinessState { get; set; } = null!;
    public string ControlMode { get; set; } = null!;
    public string? CrewSummary { get; set; }
    public string? ProbeCoreValue { get; set; }
    public WarningDto[] Warnings { get; set; } = Array.Empty<WarningDto>();
}
