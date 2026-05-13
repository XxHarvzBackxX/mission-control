using Microsoft.AspNetCore.Mvc;
using MissionControl.Api.DTOs;
using MissionControl.Domain;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Interfaces;
using MissionControl.Domain.Services;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RocketsController : ControllerBase
{
    private readonly IRocketRepository _rocketRepo;
    private readonly IPartCatalogueRepository _partsRepo;
    private readonly ICelestialBodyRepository _bodiesRepo;

    public RocketsController(
        IRocketRepository rocketRepo,
        IPartCatalogueRepository partsRepo,
        ICelestialBodyRepository bodiesRepo)
    {
        _rocketRepo = rocketRepo;
        _partsRepo = partsRepo;
        _bodiesRepo = bodiesRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RocketListItemDto>>> GetAll()
    {
        var rockets = await _rocketRepo.GetAllAsync();
        var parts = await _partsRepo.GetAllAsync();
        var kerbin = await _bodiesRepo.GetByIdAsync("kerbin");

        var items = rockets.Select(r =>
        {
            RocketDeltaVBreakdownDto? breakdown = null;
            if (kerbin != null)
            {
                var result = RocketDeltaVCalculator.Calculate(r, parts, kerbin);
                breakdown = MapBreakdown(result);
            }
            return new RocketListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                StageCount = r.Stages.Count,
                UsesAsparagusStaging = r.UsesAsparagusStaging,
                TotalEffectiveDeltaV = breakdown?.TotalEffectiveDeltaV,
                HasWarnings = breakdown?.Warnings.Count > 0 || breakdown?.Stages.Any(s => s.Warnings.Count > 0) == true,
                IsValid = breakdown?.IsValid ?? false
            };
        }).ToList();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<RocketSummaryDto>> Create([FromBody] CreateRocketDto dto)
    {
        var existing = await _rocketRepo.GetByNameAsync(dto.Name);
        if (existing != null)
            return Conflict(new { errors = new[] { new { field = "name", message = $"A rocket named '{dto.Name}' already exists." } } });

        Rocket rocket;
        try
        {
            var stages = MapStages(dto.Stages);
            rocket = Rocket.Create(dto.Name, dto.Description, stages,
                dto.UsesAsparagusStaging, dto.AsparagusEfficiencyBonus, dto.Notes);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { errors = new[] { new { field = "", message = ex.Message } } });
        }

        await _rocketRepo.AddAsync(rocket);

        var summary = await BuildSummaryAsync(rocket);
        return CreatedAtAction(nameof(GetById), new { id = rocket.Id }, summary);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RocketSummaryDto>> GetById(Guid id)
    {
        var rocket = await _rocketRepo.GetByIdAsync(id);
        if (rocket == null)
            return NotFound(new { message = $"Rocket '{id}' not found." });

        var summary = await BuildSummaryAsync(rocket);
        return Ok(summary);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RocketSummaryDto>> Update(Guid id, [FromBody] UpdateRocketDto dto)
    {
        var rocket = await _rocketRepo.GetByIdAsync(id);
        if (rocket == null)
            return NotFound(new { message = $"Rocket '{id}' not found." });

        // Check name uniqueness if changed
        if (!string.Equals(rocket.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameConflict = await _rocketRepo.GetByNameAsync(dto.Name);
            if (nameConflict != null)
                return Conflict(new { errors = new[] { new { field = "name", message = $"A rocket named '{dto.Name}' already exists." } } });
        }

        try
        {
            var stages = MapStages(dto.Stages);
            rocket.Update(dto.Name, dto.Description, stages,
                dto.UsesAsparagusStaging, dto.AsparagusEfficiencyBonus, dto.Notes);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { errors = new[] { new { field = "", message = ex.Message } } });
        }

        await _rocketRepo.UpdateAsync(rocket);

        var summary = await BuildSummaryAsync(rocket);
        return Ok(summary);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var rocket = await _rocketRepo.GetByIdAsync(id);
        if (rocket == null)
            return NotFound(new { message = $"Rocket '{id}' not found." });

        var missionIds = await _rocketRepo.GetMissionIdsAssignedToRocketAsync(id);
        await _rocketRepo.DeleteAsync(id);

        return Ok(new { affectedMissionCount = missionIds.Count });
    }

    private async Task<RocketSummaryDto> BuildSummaryAsync(Rocket rocket)
    {
        var parts = await _partsRepo.GetAllAsync();
        var kerbin = await _bodiesRepo.GetByIdAsync("kerbin");

        RocketDeltaVBreakdownDto? breakdown = null;
        if (kerbin != null)
        {
            var result = RocketDeltaVCalculator.Calculate(rocket, parts, kerbin);
            breakdown = MapBreakdown(result);
        }

        return new RocketSummaryDto
        {
            Id = rocket.Id,
            Name = rocket.Name,
            Description = rocket.Description,
            Notes = rocket.Notes,
            UsesAsparagusStaging = rocket.UsesAsparagusStaging,
            AsparagusEfficiencyBonus = rocket.AsparagusEfficiencyBonus,
            Stages = rocket.Stages.Select(s => new StageDto
            {
                Id = s.Id,
                StageNumber = s.StageNumber,
                Name = s.Name,
                IsJettisoned = s.IsJettisoned,
                Notes = s.Notes,
                Parts = s.Parts.Select(p => new StageEntryDto
                {
                    PartId = p.PartId,
                    Quantity = p.Quantity
                }).ToList()
            }).ToList(),
            DeltaVBreakdown = breakdown
        };
    }

    private static RocketDeltaVBreakdownDto MapBreakdown(
        MissionControl.Domain.ValueObjects.RocketDeltaVResult result) => new()
    {
        TotalEffectiveDeltaV = result.TotalEffectiveDeltaV,
        IsValid = result.IsValid,
        Stages = result.Stages.Select(s => new StageDeltaVDto
        {
            StageNumber = s.StageNumber,
            StageName = s.StageName,
            WetMass = s.WetMass,
            DryMass = s.DryMass,
            IspUsed = s.IspUsed,
            RawDeltaV = s.RawDeltaV,
            EfficiencyFactor = s.EfficiencyFactor,
            AsparagusBonus = s.AsparagusBonus,
            EffectiveDeltaV = s.EffectiveDeltaV,
            Warnings = s.Warnings.Select(w => new WarningDto
            {
                Type = w.Type.ToString(),
                Message = w.Message,
                IsBlocking = w.IsBlocking
            }).ToList()
        }).ToList(),
        Warnings = result.Warnings.Select(w => new WarningDto
        {
            Type = w.Type.ToString(),
            Message = w.Message,
            IsBlocking = w.IsBlocking
        }).ToList()
    };

    private static IReadOnlyList<Stage> MapStages(IEnumerable<CreateStageDto> dtos) =>
        dtos.Select(s => Stage.Create(
            s.StageNumber,
            s.Name,
            s.Parts.Select(p => new StageEntry(p.PartId, p.Quantity)).ToList(),
            s.IsJettisoned,
            s.Notes)).ToList();
}
