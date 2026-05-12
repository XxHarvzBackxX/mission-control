namespace MissionControl.Api.DTOs;

public class WarningDto
{
    public string Type { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsBlocking { get; set; }
}
