using Microsoft.AspNetCore.Mvc;
using MissionControl.Api.DTOs;
using MissionControl.Domain;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Interfaces;
using MissionControl.Domain.Services;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionsController : ControllerBase
{
    private readonly IMissionRepository _repository;
    private readonly IRocketRepository _rocketRepo;
    private readonly IPartCatalogueRepository _partsRepo;
    private readonly ICelestialBodyRepository _bodiesRepo;

    public MissionsController(
        IMissionRepository repository,
        IRocketRepository rocketRepo,
        IPartCatalogueRepository partsRepo,
        ICelestialBodyRepository bodiesRepo)
    {
        _repository = repository;
        _rocketRepo = rocketRepo;
        _partsRepo = partsRepo;
        _bodiesRepo = bodiesRepo;
    }

    [HttpGet("reference-data")]
    public ActionResult<ReferenceDataDto> GetReferenceData()
    {
        return Ok(new ReferenceDataDto
        {
            TargetBodies = KspBodyValue.TargetBodies.ToArray(),
            MissionTypes = KspBodyValue.MissionTypes.ToArray(),
            ProbeCores = KspBodyValue.ProbeCores.ToArray()
        });
    }

    [HttpPost]
    public async Task<ActionResult<MissionSummaryDto>> Create([FromBody] CreateMissionDto dto)
    {
        var existing = await _repository.GetByNameAsync(dto.Name);
        if (existing != null)
            return Conflict(new { errors = new[] { new { field = "name", message = $"A mission named '{dto.Name}' already exists." } } });

        Mission mission;
        try
        {
            mission = Mission.Create(
                dto.Name,
                new KspBodyValue(dto.TargetBodyValue, dto.TargetBodyIsCustom),
                new KspBodyValue(dto.MissionTypeValue, dto.MissionTypeIsCustom),
                dto.AvailableDeltaV,
                dto.RequiredDeltaV,
                ParseControlMode(dto.ControlMode),
                dto.CrewMembers,
                ParseProbeCore(dto.ProbeCoreValue, dto.ProbeCoreIsCustom),
                dto.StartMissionTime.HasValue ? new KerbinTime(dto.StartMissionTime.Value) : null,
                dto.EndMissionTime.HasValue ? new KerbinTime(dto.EndMissionTime.Value) : null);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { errors = new[] { new { field = "", message = ex.Message } } });
        }

        if (dto.AssignedRocketId.HasValue)
        {
            var rocketResult = await AssignRocketToMissionAsync(mission, dto.AssignedRocketId.Value, dto.CalculationProfile);
            if (rocketResult != null) return rocketResult;
        }

        await _repository.AddAsync(mission);

        var summary = await BuildSummaryAsync(mission);
        return CreatedAtAction(nameof(GetById), new { id = mission.Id }, summary);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MissionSummaryDto>> GetById(Guid id)
    {
        var mission = await _repository.GetByIdAsync(id);
        if (mission == null)
            return NotFound(new { message = $"Mission '{id}' not found." });

        var summary = await BuildSummaryAsync(mission);
        return Ok(summary);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MissionListItemDto>>> GetAll()
    {
        var missions = await _repository.GetAllAsync();
        return Ok(missions.Select(ToListItemDto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MissionSummaryDto>> Update(Guid id, [FromBody] UpdateMissionDto dto)
    {
        var mission = await _repository.GetByIdAsync(id);
        if (mission == null)
            return NotFound(new { message = $"Mission '{id}' not found." });

        var existingByName = await _repository.GetByNameAsync(dto.Name);
        if (existingByName != null && existingByName.Id != id)
            return Conflict(new { errors = new[] { new { field = "name", message = $"A mission named '{dto.Name}' already exists." } } });

        try
        {
            mission.Update(
                dto.Name,
                new KspBodyValue(dto.TargetBodyValue, dto.TargetBodyIsCustom),
                new KspBodyValue(dto.MissionTypeValue, dto.MissionTypeIsCustom),
                dto.AvailableDeltaV,
                dto.RequiredDeltaV,
                ParseControlMode(dto.ControlMode),
                dto.CrewMembers,
                ParseProbeCore(dto.ProbeCoreValue, dto.ProbeCoreIsCustom),
                dto.StartMissionTime.HasValue ? new KerbinTime(dto.StartMissionTime.Value) : null,
                dto.EndMissionTime.HasValue ? new KerbinTime(dto.EndMissionTime.Value) : null);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { errors = new[] { new { field = "", message = ex.Message } } });
        }

        if (dto.AssignedRocketId.HasValue)
        {
            var rocketResult = await AssignRocketToMissionAsync(mission, dto.AssignedRocketId.Value, dto.CalculationProfile);
            if (rocketResult != null) return rocketResult;
        }
        else
        {
            // Clear any previous rocket assignment
            mission.AssignRocket(null, null, null);
        }

        await _repository.UpdateAsync(mission);
        var summary = await BuildSummaryAsync(mission);
        return Ok(summary);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var mission = await _repository.GetByIdAsync(id);
        if (mission == null)
            return NotFound(new { message = $"Mission '{id}' not found." });

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    private static MissionControlMode ParseControlMode(string mode)
    {
        if (Enum.TryParse<MissionControlMode>(mode, true, out var result))
            return result;
        throw new DomainException($"Invalid control mode: '{mode}'. Must be 'Crewed' or 'Probe'.");
    }

    private static KspBodyValue? ParseProbeCore(string? value, bool isCustom)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return new KspBodyValue(value, isCustom);
    }

    private async Task<ActionResult?> AssignRocketToMissionAsync(
        Mission mission, Guid rocketId, MissionCalculationProfileDto? profileDto)
    {
        var rocket = await _rocketRepo.GetByIdAsync(rocketId);
        if (rocket == null)
            return BadRequest(new { errors = new[] { new { field = "assignedRocketId", message = $"Rocket '{rocketId}' not found." } } });

        MissionCalculationProfile? profile = null;
        if (profileDto != null &&
            Enum.TryParse<MissionProfileType>(profileDto.ProfileType, ignoreCase: true, out var profileType))
        {
            profile = new MissionCalculationProfile(
                profileDto.LaunchBodyId,
                profileDto.TargetBodyId,
                profileType,
                profileDto.TargetOrbitAltitude,
                profileDto.AtmosphericEfficiencyMultiplier,
                profileDto.SafetyMarginPercent,
                profileDto.RequiredDeltaVOverride);
        }

        mission.AssignRocket(rocketId, rocket.Name, profile);
        return null;
    }

    private async Task<MissionSummaryDto> BuildSummaryAsync(Mission mission)
    {
        var reserveMargin = (mission.AvailableDeltaV - mission.RequiredDeltaV) / mission.RequiredDeltaV * 100.0;
        var dto = new MissionSummaryDto
        {
            Id = mission.Id,
            Name = mission.Name,
            TargetBodyValue = mission.TargetBody.Value,
            TargetBodyIsCustom = mission.TargetBody.IsCustom,
            MissionTypeValue = mission.MissionType.Value,
            MissionTypeIsCustom = mission.MissionType.IsCustom,
            AvailableDeltaV = mission.AvailableDeltaV,
            RequiredDeltaV = mission.RequiredDeltaV,
            ReserveMarginPercent = Math.Round(reserveMargin, 2),
            ReadinessState = mission.ReadinessState.ToString(),
            ControlMode = mission.ControlMode.ToString(),
            CrewMembers = mission.CrewMembers.ToArray(),
            ProbeCoreValue = mission.ProbeCore?.Value,
            ProbeCoreIsCustom = mission.ProbeCore?.IsCustom ?? false,
            StartMissionTime = mission.StartMissionTime?.TotalSeconds,
            EndMissionTime = mission.EndMissionTime?.TotalSeconds,
            Warnings = mission.Warnings.Select(w => new WarningDto
            {
                Type = w.Type.ToString(),
                Message = w.Message,
                IsBlocking = w.IsBlocking
            }).ToArray(),
            AssignedRocketId = mission.AssignedRocketId,
            RocketName = mission.RocketName,
            CalculationProfile = mission.CalculationProfile == null ? null : new MissionCalculationProfileDto
            {
                LaunchBodyId = mission.CalculationProfile.LaunchBodyId,
                TargetBodyId = mission.CalculationProfile.TargetBodyId,
                ProfileType = mission.CalculationProfile.ProfileType.ToString(),
                TargetOrbitAltitude = mission.CalculationProfile.TargetOrbitAltitude,
                AtmosphericEfficiencyMultiplier = mission.CalculationProfile.AtmosphericEfficiencyMultiplier,
                SafetyMarginPercent = mission.CalculationProfile.SafetyMarginPercent,
                RequiredDeltaVOverride = mission.CalculationProfile.RequiredDeltaVOverride
            }
        };

        // Compute required delta-v breakdown if a calculation profile is set
        if (mission.CalculationProfile != null)
        {
            var launchBody = await _bodiesRepo.GetByIdAsync(mission.CalculationProfile.LaunchBodyId);
            var targetBody = await _bodiesRepo.GetByIdAsync(mission.CalculationProfile.TargetBodyId);

            if (launchBody != null && targetBody != null)
            {
                var breakdown = CelestialBodyDeltaVEstimator.Estimate(launchBody, targetBody, mission.CalculationProfile);
                dto.RequiredDeltaVBreakdown = new RequiredDeltaVBreakdownDto
                {
                    TotalRequiredDeltaV = breakdown.TotalRequiredDeltaV,
                    AscentDeltaV = breakdown.AscentDeltaV,
                    TransferDeltaV = breakdown.TransferDeltaV,
                    DescentDeltaV = breakdown.DescentDeltaV,
                    ReturnDeltaV = breakdown.ReturnDeltaV,
                    EstimationMethod = breakdown.EstimationMethod,
                    IsApproximated = breakdown.IsApproximated
                };
            }
        }

        return dto;
    }

    private static MissionSummaryDto ToSummaryDto(Mission m)
    {
        var reserveMargin = (m.AvailableDeltaV - m.RequiredDeltaV) / m.RequiredDeltaV * 100.0;
        return new MissionSummaryDto
        {
            Id = m.Id,
            Name = m.Name,
            TargetBodyValue = m.TargetBody.Value,
            TargetBodyIsCustom = m.TargetBody.IsCustom,
            MissionTypeValue = m.MissionType.Value,
            MissionTypeIsCustom = m.MissionType.IsCustom,
            AvailableDeltaV = m.AvailableDeltaV,
            RequiredDeltaV = m.RequiredDeltaV,
            ReserveMarginPercent = Math.Round(reserveMargin, 2),
            ReadinessState = m.ReadinessState.ToString(),
            ControlMode = m.ControlMode.ToString(),
            CrewMembers = m.CrewMembers.ToArray(),
            ProbeCoreValue = m.ProbeCore?.Value,
            ProbeCoreIsCustom = m.ProbeCore?.IsCustom ?? false,
            StartMissionTime = m.StartMissionTime?.TotalSeconds,
            EndMissionTime = m.EndMissionTime?.TotalSeconds,
            Warnings = m.Warnings.Select(w => new WarningDto
            {
                Type = w.Type.ToString(),
                Message = w.Message,
                IsBlocking = w.IsBlocking
            }).ToArray(),
            AssignedRocketId = m.AssignedRocketId,
            RocketName = m.RocketName
        };
    }

    private static MissionListItemDto ToListItemDto(Mission m)
    {
        string? crewSummary = null;
        if (m.ControlMode == MissionControlMode.Crewed && m.CrewMembers.Count > 0)
        {
            crewSummary = m.CrewMembers.Count == 1
                ? m.CrewMembers[0]
                : $"{m.CrewMembers[0]} +{m.CrewMembers.Count - 1}";
        }

        return new MissionListItemDto
        {
            Id = m.Id,
            Name = m.Name,
            ReadinessState = m.ReadinessState.ToString(),
            ControlMode = m.ControlMode.ToString(),
            CrewSummary = crewSummary,
            ProbeCoreValue = m.ProbeCore?.Value,
            Warnings = m.Warnings.Select(w => new WarningDto
            {
                Type = w.Type.ToString(),
                Message = w.Message,
                IsBlocking = w.IsBlocking
            }).ToArray(),
            AssignedRocketId = m.AssignedRocketId,
            RocketName = m.RocketName
        };
    }
}
