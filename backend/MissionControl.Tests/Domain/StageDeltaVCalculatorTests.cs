using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.Services;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Tests.Domain;

[TestFixture]
public class StageDeltaVCalculatorTests
{
    private const double G0 = 9.80665;

    private static CataloguePart MakeEngine(string id, double ispSl, double ispVac, double thrust,
        FuelType fuelType = FuelType.LiquidFuelOxidizer) => new()
    {
        Id = id,
        Name = id,
        Category = PartCategory.Engines,
        DryMass = 1.0,
        WetMass = 1.0,
        EngineStats = new EngineStats
        {
            ThrustSeaLevel = thrust,
            ThrustVacuum = thrust * 1.2,
            IspSeaLevel = ispSl,
            IspVacuum = ispVac,
            FuelTypes = new[] { fuelType }
        }
    };

    private static CataloguePart MakeTank(string id, double dryMass, double wetMass,
        FuelType fuelType = FuelType.LiquidFuelOxidizer) => new()
    {
        Id = id,
        Name = id,
        Category = PartCategory.FuelTanks,
        DryMass = dryMass,
        WetMass = wetMass,
        FuelCapacity = new Dictionary<FuelType, double> { [fuelType] = (wetMass - dryMass) * 100 }
    };

    private static Stage MakeStage(int number, params (string partId, int qty)[] parts) =>
        Stage.Create(number, $"Stage {number}",
            parts.Select(p => new StageEntry(p.partId, p.qty)).ToList());

    [Test]
    public void Calculate_SingleEngineAndTank_ReturnsCorrectDeltaV()
    {
        var engine = MakeEngine("lv-t45", 270, 320, 200);
        var tank = MakeTank("fl-t400", 0.25, 2.25);
        var pod = new CataloguePart
        {
            Id = "mk1-pod", Name = "Mk1 Pod", Category = PartCategory.Pods,
            DryMass = 0.84, WetMass = 0.84
        };
        var parts = new List<CataloguePart> { engine, tank, pod };
        var stage = MakeStage(1, ("lv-t45", 1), ("fl-t400", 1), ("mk1-pod", 1));

        // wet mass = 1.0 + 2.25 + 0.84 = 4.09
        double wetMass = 4.09;
        // dry mass = 1.0 + 0.25 + 0.84 = 2.09
        // expected raw DV = 270 × 9.80665 × ln(4.09/2.09) ≈ 1826.2 × 0.6717 ≈ 1226 m/s
        // With eff=0.85: ~1041 m/s

        var result = StageDeltaVCalculator.Calculate(stage, parts, wetMass,
            useVacuumIsp: false, efficiencyFactor: 0.85, asparagusBonus: 0.0);

        Assert.That(result.IsValid(), Is.True, "Result should be valid");
        Assert.That(result.EffectiveDeltaV, Is.GreaterThan(0));
        Assert.That(result.IspUsed, Is.EqualTo(270).Within(0.01));
    }

    [Test]
    public void Calculate_NoEngine_ReturnsBlockingWarning()
    {
        var tank = MakeTank("tank1", 0.25, 2.25);
        var parts = new List<CataloguePart> { tank };
        var stage = MakeStage(1, ("tank1", 1));

        var result = StageDeltaVCalculator.Calculate(stage, parts, 2.25,
            useVacuumIsp: false, efficiencyFactor: 1.0, asparagusBonus: 0.0);

        Assert.That(result.Warnings.Any(w => w.Type == WarningType.NoEngine && w.IsBlocking));
        Assert.That(result.EffectiveDeltaV, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_NoFuel_ReturnsBlockingWarning()
    {
        var engine = MakeEngine("engine1", 270, 320, 200);
        var parts = new List<CataloguePart> { engine };
        var stage = MakeStage(1, ("engine1", 1));

        var result = StageDeltaVCalculator.Calculate(stage, parts, 1.0,
            useVacuumIsp: false, efficiencyFactor: 1.0, asparagusBonus: 0.0);

        Assert.That(result.Warnings.Any(w => w.Type == WarningType.NoFuelSource && w.IsBlocking));
    }

    [Test]
    public void Calculate_VacuumIsp_UsesVacuumValues()
    {
        var engine = MakeEngine("lv-909", 85, 345, 50);
        var tank = MakeTank("tank1", 0.25, 2.25);
        var parts = new List<CataloguePart> { engine, tank };
        var stage = MakeStage(1, ("lv-909", 1), ("tank1", 1));
        double wetMass = 3.25;

        var vacResult = StageDeltaVCalculator.Calculate(stage, parts, wetMass,
            useVacuumIsp: true, efficiencyFactor: 1.0, asparagusBonus: 0.0);
        var slResult = StageDeltaVCalculator.Calculate(stage, parts, wetMass,
            useVacuumIsp: false, efficiencyFactor: 1.0, asparagusBonus: 0.0);

        Assert.That(vacResult.IspUsed, Is.EqualTo(345).Within(0.01));
        Assert.That(slResult.IspUsed, Is.EqualTo(85).Within(0.01));
        Assert.That(vacResult.EffectiveDeltaV, Is.GreaterThan(slResult.EffectiveDeltaV));
    }

    [Test]
    public void Calculate_AsparagusBonus_IncreasesEffectiveDeltaV()
    {
        var engine = MakeEngine("lv-t45", 270, 320, 200);
        var tank = MakeTank("fl-t400", 0.25, 2.25);
        var parts = new List<CataloguePart> { engine, tank };
        var stage = MakeStage(1, ("lv-t45", 1), ("fl-t400", 1));
        double wetMass = 3.25;

        var noBonus = StageDeltaVCalculator.Calculate(stage, parts, wetMass,
            useVacuumIsp: false, efficiencyFactor: 0.85, asparagusBonus: 0.0);
        var withBonus = StageDeltaVCalculator.Calculate(stage, parts, wetMass,
            useVacuumIsp: false, efficiencyFactor: 0.85, asparagusBonus: 0.08);

        Assert.That(withBonus.EffectiveDeltaV, Is.GreaterThan(noBonus.EffectiveDeltaV));
        Assert.That(withBonus.EffectiveDeltaV, Is.EqualTo(noBonus.EffectiveDeltaV * 1.08).Within(0.01));
    }

    [Test]
    public void Calculate_ThrustWeightedIsp_MultipleEngines()
    {
        var engine1 = MakeEngine("eng1", 270, 320, 200);
        var engine2 = MakeEngine("eng2", 250, 300, 100);
        var tank = MakeTank("tank1", 0.25, 2.25);
        var parts = new List<CataloguePart> { engine1, engine2, tank };
        var stage = MakeStage(1, ("eng1", 1), ("eng2", 1), ("tank1", 1));

        // Thrust-weighted Isp = (200 + 100) / (200/270 + 100/250) = 300 / (0.7407 + 0.4) = 300 / 1.1407 ≈ 263.0
        double wetMass = 4.25;
        var result = StageDeltaVCalculator.Calculate(stage, parts, wetMass,
            useVacuumIsp: false, efficiencyFactor: 1.0, asparagusBonus: 0.0);

        double expectedIsp = (200.0 + 100.0) / (200.0 / 270.0 + 100.0 / 250.0);
        Assert.That(result.IspUsed, Is.EqualTo(expectedIsp).Within(0.01));
    }
}

public static class StageDeltaVResultExtensions
{
    public static bool IsValid(this StageDeltaVResult result) =>
        !result.Warnings.Any(w => w.IsBlocking);
}
