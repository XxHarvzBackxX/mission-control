namespace MissionControl.Api.DTOs;

public class StageDto
{
    public Guid Id { get; set; }
    public int StageNumber { get; set; }
    public string Name { get; set; } = null!;
    public bool IsJettisoned { get; set; }
    public string? Notes { get; set; }
    public List<StageEntryDto> Parts { get; set; } = new();
}

public class RocketListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int StageCount { get; set; }
    public bool UsesAsparagusStaging { get; set; }
    public double? TotalEffectiveDeltaV { get; set; }
    public bool HasWarnings { get; set; }
    public bool IsValid { get; set; }
}

public class StageDeltaVDto
{
    public int StageNumber { get; set; }
    public string StageName { get; set; } = null!;
    public double WetMass { get; set; }
    public double DryMass { get; set; }
    public double IspUsed { get; set; }
    public double RawDeltaV { get; set; }
    public double EfficiencyFactor { get; set; }
    public double AsparagusBonus { get; set; }
    public double EffectiveDeltaV { get; set; }
    public List<WarningDto> Warnings { get; set; } = new();
}

public class RocketDeltaVBreakdownDto
{
    public double TotalEffectiveDeltaV { get; set; }
    public bool IsValid { get; set; }
    public List<StageDeltaVDto> Stages { get; set; } = new();
    public List<WarningDto> Warnings { get; set; } = new();
}

public class RocketSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Notes { get; set; }
    public bool UsesAsparagusStaging { get; set; }
    public double AsparagusEfficiencyBonus { get; set; }
    public List<StageDto> Stages { get; set; } = new();
    public RocketDeltaVBreakdownDto? DeltaVBreakdown { get; set; }
}
