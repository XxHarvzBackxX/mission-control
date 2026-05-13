using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Services;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Tests.Domain;

[TestFixture]
public class RocketDeltaVCalculatorTests
{
    private static CelestialBody Kerbin { get; } = new()
    {
        Id = "kerbin",
        Name = "Kerbin",
        EquatorialRadius = 600_000,
        SurfaceGravity = 9.81,
        SurfacePressure = 1.0,
        AtmosphereHeight = 70_000,
        IsCustom = false
    };

    private static CelestialBody Mun { get; } = new()
    {
        Id = "mun",
        Name = "Mun",
        EquatorialRadius = 200_000,
        SurfaceGravity = 1.63,
        SurfacePressure = 0,
        AtmosphereHeight = 0,
        IsCustom = false
    };

    private static CataloguePart LvT45 { get; } = new()
    {
        Id = "lv-t45",
        Name = "LV-T45",
        Category = PartCategory.Engines,
        DryMass = 1.5,
        WetMass = 1.5,
        EngineStats = new EngineStats
        {
            ThrustSeaLevel = 167.9,
            ThrustVacuum = 215.0,
            IspSeaLevel = 270,
            IspVacuum = 320,
            FuelTypes = new[] { FuelType.LiquidFuelOxidizer }
        }
    };

    private static CataloguePart Mk1Pod { get; } = new()
    {
        Id = "mk1-pod",
        Name = "Mk1 Command Pod",
        Category = PartCategory.Pods,
        DryMass = 0.84,
        WetMass = 0.84
    };

    private static CataloguePart FlT400 { get; } = new()
    {
        Id = "fl-t400",
        Name = "FL-T400",
        Category = PartCategory.FuelTanks,
        DryMass = 0.25,
        WetMass = 2.25,
        FuelCapacity = new Dictionary<FuelType, double> { [FuelType.LiquidFuelOxidizer] = 400 }
    };

    [Test]
    public void Calculate_SingleStageAtmospheric_ProducesPositiveDeltaV()
    {
        var stage = Stage.Create(1, "Main Stage",
            new[] { new StageEntry("lv-t45", 1), new StageEntry("fl-t400", 1), new StageEntry("mk1-pod", 1) });
        var rocket = Rocket.Create("Test Rocket", "A test rocket",
            new[] { stage }, false, 0.0);
        var parts = new List<CataloguePart> { LvT45, FlT400, Mk1Pod };

        var result = RocketDeltaVCalculator.Calculate(rocket, parts, Kerbin);

        Assert.That(result.IsValid);
        Assert.That(result.TotalEffectiveDeltaV, Is.GreaterThan(0));
        Assert.That(result.Stages, Has.Count.EqualTo(1));
    }

    [Test]
    public void Calculate_MultiStage_SumOfStages()
    {
        // Stage 2 (higher number = fires first) and Stage 1 (payload)
        var stage2 = Stage.Create(2, "Launch Stage",
            new[] { new StageEntry("lv-t45", 1), new StageEntry("fl-t400", 1) },
            isJettisoned: true);
        var stage1 = Stage.Create(1, "Payload Stage",
            new[] { new StageEntry("lv-t45", 1), new StageEntry("fl-t400", 1), new StageEntry("mk1-pod", 1) });

        var rocket = Rocket.Create("Two Stage", "Test",
            new[] { stage1, stage2 }, false, 0.0);
        var parts = new List<CataloguePart> { LvT45, FlT400, Mk1Pod };

        var result = RocketDeltaVCalculator.Calculate(rocket, parts, Kerbin);

        Assert.That(result.IsValid);
        Assert.That(result.TotalEffectiveDeltaV, Is.GreaterThan(0));
        Assert.That(result.Stages, Has.Count.EqualTo(2));

        double stageSum = result.Stages.Sum(s => s.EffectiveDeltaV);
        Assert.That(result.TotalEffectiveDeltaV, Is.EqualTo(stageSum).Within(0.01));
    }

    [Test]
    public void Calculate_VacuumBody_UsesVacuumIsp()
    {
        var lv909 = new CataloguePart
        {
            Id = "lv-909",
            Name = "LV-909",
            Category = PartCategory.Engines,
            DryMass = 0.5,
            WetMass = 0.5,
            EngineStats = new EngineStats
            {
                ThrustSeaLevel = 14.3,
                ThrustVacuum = 50.0,
                IspSeaLevel = 85,
                IspVacuum = 345,
                FuelTypes = new[] { FuelType.LiquidFuelOxidizer }
            }
        };

        var stage = Stage.Create(1, "Vac Stage",
            new[] { new StageEntry("lv-909", 1), new StageEntry("fl-t400", 1), new StageEntry("mk1-pod", 1) });
        var rocket = Rocket.Create("Vacuum Rocket", "Test", new[] { stage }, false, 0.0);
        var parts = new List<CataloguePart> { lv909, FlT400, Mk1Pod };

        var result = RocketDeltaVCalculator.Calculate(rocket, parts, Mun);

        Assert.That(result.IsValid);
        Assert.That(result.Stages[0].IspUsed, Is.EqualTo(345).Within(0.01));
    }

    [Test]
    public void Calculate_JettisonedStageReducesMass()
    {
        // Stage 2 jettisoned, Stage 1 has same parts
        // Stage 2's dry hardware is removed before Stage 1 fires
        var stage2 = Stage.Create(2, "Booster",
            new[] { new StageEntry("lv-t45", 1), new StageEntry("fl-t400", 1) },
            isJettisoned: true);
        var stage1 = Stage.Create(1, "Core",
            new[] { new StageEntry("lv-t45", 1), new StageEntry("fl-t400", 1), new StageEntry("mk1-pod", 1) });

        var rocket = Rocket.Create("Test", "Test", new[] { stage1, stage2 }, false, 0.0);
        var parts = new List<CataloguePart> { LvT45, FlT400, Mk1Pod };

        var result = RocketDeltaVCalculator.Calculate(rocket, parts, Kerbin);
        var stage1Result = result.Stages.First(s => s.StageNumber == 1);
        var stage2Result = result.Stages.First(s => s.StageNumber == 2);

        // After Stage 2 burns and jettsons, Stage 1 wet mass should be less than Stage 2 wet mass
        Assert.That(stage1Result.WetMass, Is.LessThan(stage2Result.WetMass));
    }

    [Test]
    public void Calculate_NoCommandPart_AddsWarning()
    {
        var stage = Stage.Create(1, "Probe Stage",
            new[] { new StageEntry("lv-t45", 1), new StageEntry("fl-t400", 1) });
        var rocket = Rocket.Create("No Pod Rocket", "Test", new[] { stage }, false, 0.0);
        var parts = new List<CataloguePart> { LvT45, FlT400 };

        var result = RocketDeltaVCalculator.Calculate(rocket, parts, Kerbin);

        Assert.That(result.Warnings.Any(w => w.Type == WarningType.NoCommandPart));
    }
}
