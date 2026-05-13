namespace MissionControl.Api.DTOs;

public class StageEntryDto
{
    public string PartId { get; set; } = null!;
    public int Quantity { get; set; }
}

public class CreateStageDto
{
    public int StageNumber { get; set; }
    public string Name { get; set; } = null!;
    public bool IsJettisoned { get; set; } = true;
    public string? Notes { get; set; }
    public List<StageEntryDto> Parts { get; set; } = new();
}

public class CreateRocketDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Notes { get; set; }
    public bool UsesAsparagusStaging { get; set; }
    public double AsparagusEfficiencyBonus { get; set; }
    public List<CreateStageDto> Stages { get; set; } = new();
}

public class UpdateRocketDto : CreateRocketDto { }
