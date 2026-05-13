using Microsoft.AspNetCore.Mvc;
using MissionControl.Api.DTOs;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Api.Controllers;

[ApiController]
[Route("api/celestial-bodies")]
public class CelestialBodiesController : ControllerBase
{
    private readonly ICelestialBodyRepository _repo;

    public CelestialBodiesController(ICelestialBodyRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CelestialBodyDto>>> GetAll()
    {
        var bodies = await _repo.GetAllAsync();
        return Ok(bodies.Select(MapToDto).ToList());
    }

    [HttpPost("custom")]
    public async Task<ActionResult<CelestialBodyDto>> CreateCustom([FromBody] CreateCustomBodyDto dto)
    {
        if (dto.EquatorialRadius <= 0)
            return BadRequest(new { errors = new[] { new { field = "equatorialRadius", message = "Radius must be greater than 0." } } });

        if (dto.SurfaceGravity <= 0)
            return BadRequest(new { errors = new[] { new { field = "surfaceGravity", message = "Surface gravity must be greater than 0." } } });

        if (dto.SurfacePressure < 0)
            return BadRequest(new { errors = new[] { new { field = "surfacePressure", message = "Surface pressure cannot be negative." } } });

        var body = new CelestialBody
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            ParentBodyId = dto.ParentBodyId,
            EquatorialRadius = dto.EquatorialRadius,
            SurfaceGravity = dto.SurfaceGravity,
            SurfacePressure = dto.SurfacePressure,
            AtmosphereHeight = dto.AtmosphereHeight,
            DefaultOrbitAltitude = dto.DefaultOrbitAltitude,
            IsCustom = true
        };

        await _repo.AddCustomAsync(body);

        return CreatedAtAction(nameof(GetAll), MapToDto(body));
    }

    private static CelestialBodyDto MapToDto(CelestialBody b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        ParentBodyId = b.ParentBodyId,
        EquatorialRadius = b.EquatorialRadius,
        SurfaceGravity = b.SurfaceGravity,
        SurfacePressure = b.SurfacePressure,
        AtmosphereHeight = b.AtmosphereHeight,
        SphereOfInfluence = b.SphereOfInfluence,
        SemiMajorAxis = b.SemiMajorAxis,
        DefaultOrbitAltitude = b.DefaultOrbitAltitude,
        HasAtmosphere = b.HasAtmosphere,
        IsCustom = b.IsCustom
    };
}
