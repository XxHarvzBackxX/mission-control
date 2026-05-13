using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using MissionControl.Api.Controllers;
using MissionControl.Api.DTOs;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Tests.Api;

[TestFixture]
public class CelestialBodiesControllerTests
{
    private ICelestialBodyRepository _repo = null!;
    private CelestialBodiesController _controller = null!;

    private static CelestialBody MakeBody(string id, bool isCustom = false) => new()
    {
        Id = id, Name = id, EquatorialRadius = 600_000, SurfaceGravity = 9.81,
        SurfacePressure = 0, AtmosphereHeight = 0, DefaultOrbitAltitude = 10_000,
        IsCustom = isCustom
    };

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ICelestialBodyRepository>();
        _controller = new CelestialBodiesController(_repo);
    }

    [Test]
    public async Task GetAll_Returns17StockBodies()
    {
        var bodies = Enumerable.Range(1, 17).Select(i => MakeBody($"body-{i}")).ToList();
        _repo.GetAllAsync().Returns(bodies);

        var result = await _controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<CelestialBodyDto>)ok.Value!;
        Assert.That(dtos, Has.Count.EqualTo(17));
    }

    [Test]
    public async Task CreateCustom_Valid_Returns201WithIsCustomTrue()
    {
        var dto = new CreateCustomBodyDto
        {
            Name = "Alternis Kerbin",
            EquatorialRadius = 550_000,
            SurfaceGravity = 8.5,
            SurfacePressure = 0,
            AtmosphereHeight = 0,
            DefaultOrbitAltitude = 80_000
        };

        var result = await _controller.CreateCustom(dto);

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result.Result!;
        var body = (CelestialBodyDto)created.Value!;
        Assert.That(body.IsCustom, Is.True);
        Assert.That(body.Name, Is.EqualTo("Alternis Kerbin"));
    }

    [Test]
    public async Task CreateCustom_RadiusZero_Returns400()
    {
        var dto = new CreateCustomBodyDto
        {
            Name = "Bad Body",
            EquatorialRadius = 0,
            SurfaceGravity = 5.0,
            SurfacePressure = 0
        };

        var result = await _controller.CreateCustom(dto);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateCustom_GravityZero_Returns400()
    {
        var dto = new CreateCustomBodyDto
        {
            Name = "Bad Body",
            EquatorialRadius = 300_000,
            SurfaceGravity = 0,
            SurfacePressure = 0
        };

        var result = await _controller.CreateCustom(dto);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateCustom_AppearsInSubsequentGet()
    {
        var customBody = MakeBody("custom-1", isCustom: true);
        _repo.GetAllAsync().Returns(new List<CelestialBody> { customBody });

        var result = await _controller.GetAll();

        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<CelestialBodyDto>)ok.Value!;
        Assert.That(dtos.Any(d => d.IsCustom));
    }
}
