using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using MissionControl.Api.Controllers;
using MissionControl.Api.DTOs;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Tests.Api;

[TestFixture]
public class RocketsControllerTests
{
    private IRocketRepository _rocketRepo = null!;
    private IPartCatalogueRepository _partsRepo = null!;
    private ICelestialBodyRepository _bodiesRepo = null!;
    private RocketsController _controller = null!;

    private static readonly CelestialBody KerbinBody = new()
    {
        Id = "kerbin", Name = "Kerbin",
        EquatorialRadius = 600_000, SurfaceGravity = 9.81,
        SurfacePressure = 1.0, AtmosphereHeight = 70_000,
        DefaultOrbitAltitude = 80_000, IsCustom = false
    };

    private static CreateRocketDto ValidRocketDto(string name = "Test Rocket") => new()
    {
        Name = name,
        Description = "A test rocket",
        UsesAsparagusStaging = false,
        AsparagusEfficiencyBonus = 0.0,
        Stages = new List<CreateStageDto>
        {
            new()
            {
                StageNumber = 1,
                Name = "Main Stage",
                IsJettisoned = false,
                Parts = new List<StageEntryDto>
                {
                    new() { PartId = "lv-t45", Quantity = 1 },
                    new() { PartId = "fl-t400", Quantity = 1 },
                    new() { PartId = "mk1-pod", Quantity = 1 }
                }
            }
        }
    };

    [SetUp]
    public void SetUp()
    {
        _rocketRepo = Substitute.For<IRocketRepository>();
        _partsRepo = Substitute.For<IPartCatalogueRepository>();
        _bodiesRepo = Substitute.For<ICelestialBodyRepository>();

        _partsRepo.GetAllAsync().Returns(new List<CataloguePart>());
        _bodiesRepo.GetByIdAsync("kerbin").Returns(KerbinBody);

        _controller = new RocketsController(_rocketRepo, _partsRepo, _bodiesRepo);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithList()
    {
        _rocketRepo.GetAllAsync().Returns(new List<Rocket>());

        var result = await _controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Create_ValidDto_Returns201WithSummary()
    {
        _rocketRepo.GetByNameAsync(Arg.Any<string>()).Returns((Rocket?)null);

        var result = await _controller.Create(ValidRocketDto());

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result.Result!;
        Assert.That(created.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.InstanceOf<RocketSummaryDto>());
    }

    [Test]
    public async Task Create_DuplicateName_Returns409()
    {
        var existing = Rocket.Create("Test Rocket", "Existing", MakeMinimalStages(), false, 0.0);
        _rocketRepo.GetByNameAsync("Test Rocket").Returns(existing);

        var result = await _controller.Create(ValidRocketDto("Test Rocket"));

        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _rocketRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Rocket?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetById_Found_ReturnsOkWithSummary()
    {
        var rocket = Rocket.Create("My Rocket", "Desc", MakeMinimalStages(), false, 0.0);
        _rocketRepo.GetByIdAsync(rocket.Id).Returns(rocket);

        var result = await _controller.GetById(rocket.Id);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.InstanceOf<RocketSummaryDto>());
    }

    [Test]
    public async Task Delete_NotFound_Returns404()
    {
        _rocketRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Rocket?)null);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_Found_ReturnsOkWithAffectedCount()
    {
        var rocket = Rocket.Create("Test", "Test", MakeMinimalStages(), false, 0.0);
        _rocketRepo.GetByIdAsync(rocket.Id).Returns(rocket);
        _rocketRepo.GetMissionIdsAssignedToRocketAsync(rocket.Id).Returns(new List<Guid>());

        var result = await _controller.Delete(rocket.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    private static IReadOnlyList<Stage> MakeMinimalStages() =>
        new[] { Stage.Create(1, "Stage 1", new[] { new MissionControl.Domain.ValueObjects.StageEntry("part-1", 1) }) };
}
