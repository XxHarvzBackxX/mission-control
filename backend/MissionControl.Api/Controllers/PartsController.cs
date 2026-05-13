using Microsoft.AspNetCore.Mvc;
using MissionControl.Api.DTOs;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartsController : ControllerBase
{
    private readonly IPartCatalogueRepository _repo;

    public PartsController(IPartCatalogueRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartDto>>> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        IReadOnlyList<CataloguePart> parts;

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts = await _repo.SearchByNameAsync(search);
        }
        else if (!string.IsNullOrWhiteSpace(category) &&
                 Enum.TryParse<PartCategory>(category, ignoreCase: true, out var cat))
        {
            parts = await _repo.GetByCategoryAsync(cat);
        }
        else
        {
            parts = await _repo.GetAllAsync();
        }

        return Ok(parts.Select(MapToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PartDto>> GetById(string id)
    {
        var part = await _repo.GetByIdAsync(id);
        if (part == null)
            return NotFound(new { message = $"Part '{id}' not found." });

        return Ok(MapToDto(part));
    }

    private static PartDto MapToDto(CataloguePart p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Category = p.Category.ToString(),
        DryMass = p.DryMass,
        WetMass = p.WetMass,
        FuelCapacity = p.FuelCapacity?.ToDictionary(
            kv => kv.Key.ToString(),
            kv => kv.Value),
        EngineStats = p.EngineStats == null ? null : new EngineStatsDto
        {
            ThrustSeaLevel = p.EngineStats.ThrustSeaLevel,
            ThrustVacuum = p.EngineStats.ThrustVacuum,
            IspSeaLevel = p.EngineStats.IspSeaLevel,
            IspVacuum = p.EngineStats.IspVacuum,
            FuelTypes = p.EngineStats.FuelTypes.Select(f => f.ToString()).ToList()
        }
    };
}
