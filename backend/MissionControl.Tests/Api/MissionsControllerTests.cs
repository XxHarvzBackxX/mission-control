using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using MissionControl.Api.Controllers;
using MissionControl.Api.DTOs;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Interfaces;
using MissionControl.Domain.ValueObjects;
using MissionControl.Domain.Enums;

namespace MissionControl.Tests.Api;

[TestFixture]
public class MissionsControllerTests
{
    private IMissionRepository _repo = null!;
    private MissionsController _controller = null!;

    private static CreateMissionDto ValidCrewedDto() => new()
    {
        Name = "Mun Landing",
        TargetBodyValue = "Mun",
        TargetBodyIsCustom = false,
        MissionTypeValue = "Landing",
        MissionTypeIsCustom = false,
        AvailableDeltaV = 5200,
        RequiredDeltaV = 4500,
        ControlMode = "Crewed",
        CrewMembers = ["Jebediah"],
        ProbeCoreValue = null,
        ProbeCoreIsCustom = false,
        StartMissionTime = null,
        EndMissionTime = null
    };

    private static Mission CreateTestMission(string name = "Mun Landing")
    {
        return Mission.Create(
            name,
            new KspBodyValue("Mun", false),
            new KspBodyValue("Landing", false),
            5200, 4500,
            MissionControlMode.Crewed,
            ["Jebediah"],
            null, null, null);
    }

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IMissionRepository>();
        _controller = new MissionsController(_repo);
    }

    // --- POST ---

    [Test]
    public async Task Create_ValidDto_Returns201()
    {
        _repo.GetByNameAsync("Mun Landing").Returns((Mission?)null);

        var result = await _controller.Create(ValidCrewedDto());

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.StatusCode, Is.EqualTo(201));
        await _repo.Received(1).AddAsync(Arg.Any<Mission>());
    }

    [Test]
    public async Task Create_DuplicateName_Returns409()
    {
        _repo.GetByNameAsync("Mun Landing").Returns(CreateTestMission());

        var result = await _controller.Create(ValidCrewedDto());

        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task Create_InvalidDomain_Returns400()
    {
        _repo.GetByNameAsync(Arg.Any<string>()).Returns((Mission?)null);
        var dto = ValidCrewedDto();
        dto.ControlMode = "Probe";
        dto.ProbeCoreValue = null; // missing probe core

        var result = await _controller.Create(dto);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // --- GET by ID ---

    [Test]
    public async Task GetById_Exists_Returns200()
    {
        var mission = CreateTestMission();
        _repo.GetByIdAsync(mission.Id).Returns(mission);

        var result = await _controller.GetById(mission.Id);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var dto = ((OkObjectResult)result.Result!).Value as MissionSummaryDto;
        Assert.That(dto!.Name, Is.EqualTo("Mun Landing"));
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Mission?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- PUT ---

    [Test]
    public async Task Update_Valid_Returns200()
    {
        var mission = CreateTestMission();
        _repo.GetByIdAsync(mission.Id).Returns(mission);
        _repo.GetByNameAsync("Mun Landing").Returns(mission);

        var dto = new UpdateMissionDto
        {
            Name = "Mun Landing",
            TargetBodyValue = "Mun", TargetBodyIsCustom = false,
            MissionTypeValue = "Landing", MissionTypeIsCustom = false,
            AvailableDeltaV = 6000, RequiredDeltaV = 4500,
            ControlMode = "Crewed", CrewMembers = ["Jebediah"],
            ProbeCoreValue = null, ProbeCoreIsCustom = false,
            StartMissionTime = null, EndMissionTime = null
        };

        var result = await _controller.Update(mission.Id, dto);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        await _repo.Received(1).UpdateAsync(mission);
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Mission?)null);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateMissionDto
        {
            Name = "X", TargetBodyValue = "Mun", TargetBodyIsCustom = false,
            MissionTypeValue = "Landing", MissionTypeIsCustom = false,
            AvailableDeltaV = 5000, RequiredDeltaV = 4000,
            ControlMode = "Crewed", CrewMembers = ["Jeb"],
            ProbeCoreValue = null, ProbeCoreIsCustom = false,
            StartMissionTime = null, EndMissionTime = null
        });

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Update_DuplicateName_Returns409()
    {
        var mission = CreateTestMission("Alpha");
        var other = CreateTestMission("Beta");
        _repo.GetByIdAsync(mission.Id).Returns(mission);
        _repo.GetByNameAsync("Beta").Returns(other);

        var dto = new UpdateMissionDto
        {
            Name = "Beta", TargetBodyValue = "Mun", TargetBodyIsCustom = false,
            MissionTypeValue = "Landing", MissionTypeIsCustom = false,
            AvailableDeltaV = 5000, RequiredDeltaV = 4000,
            ControlMode = "Crewed", CrewMembers = ["Jeb"],
            ProbeCoreValue = null, ProbeCoreIsCustom = false,
            StartMissionTime = null, EndMissionTime = null
        };

        var result = await _controller.Update(mission.Id, dto);

        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    // --- DELETE ---

    [Test]
    public async Task Delete_Exists_Returns204()
    {
        var mission = CreateTestMission();
        _repo.GetByIdAsync(mission.Id).Returns(mission);

        var result = await _controller.Delete(mission.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await _repo.Received(1).DeleteAsync(mission.Id);
    }

    [Test]
    public async Task Delete_NotFound_Returns404()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Mission?)null);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }
}
