using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using MissionControl.Api.Controllers;
using MissionControl.Api.DTOs;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Interfaces;

namespace MissionControl.Tests.Api;

[TestFixture]
public class PartsControllerTests
{
    private IPartCatalogueRepository _repo = null!;
    private PartsController _controller = null!;

    private static CataloguePart MakePart(string id, PartCategory category) => new()
    {
        Id = id, Name = $"Part {id}", Category = category, DryMass = 1.0, WetMass = 1.0
    };

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPartCatalogueRepository>();
        _controller = new PartsController(_repo);
    }

    [Test]
    public async Task GetAll_NoFilter_ReturnsAllParts()
    {
        var parts = new List<CataloguePart>
        {
            MakePart("p1", PartCategory.Engines),
            MakePart("p2", PartCategory.FuelTanks)
        };
        _repo.GetAllAsync().Returns(parts);

        var result = await _controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<PartDto>)ok.Value!;
        Assert.That(dtos, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAll_CategoryFilter_ReturnsFilteredParts()
    {
        var engines = new List<CataloguePart> { MakePart("e1", PartCategory.Engines) };
        _repo.GetByCategoryAsync(PartCategory.Engines).Returns(engines);

        var result = await _controller.GetAll(category: "Engines");

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<PartDto>)ok.Value!;
        Assert.That(dtos, Has.Count.EqualTo(1));
        Assert.That(dtos[0].Id, Is.EqualTo("e1"));
    }

    [Test]
    public async Task GetAll_SearchFilter_ReturnsMatchingParts()
    {
        var found = new List<CataloguePart> { MakePart("lv-t45", PartCategory.Engines) };
        _repo.SearchByNameAsync("lv").Returns(found);

        var result = await _controller.GetAll(search: "lv");

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<PartDto>)ok.Value!;
        Assert.That(dtos[0].Id, Is.EqualTo("lv-t45"));
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.GetByIdAsync("unknown").Returns((CataloguePart?)null);

        var result = await _controller.GetById("unknown");

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetById_Found_ReturnsDto()
    {
        var part = MakePart("lv-t45", PartCategory.Engines);
        _repo.GetByIdAsync("lv-t45").Returns(part);

        var result = await _controller.GetById("lv-t45");

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dto = (PartDto)ok.Value!;
        Assert.That(dto.Id, Is.EqualTo("lv-t45"));
    }
}
