namespace MissionControl.Api.DTOs;

public class ReferenceDataDto
{
    public string[] TargetBodies { get; set; } = Array.Empty<string>();
    public string[] MissionTypes { get; set; } = Array.Empty<string>();
    public string[] ProbeCores { get; set; } = Array.Empty<string>();
}
