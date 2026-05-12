using MissionControl.Domain.Enums;
using MissionControl.Domain.Services;

namespace MissionControl.Tests.Domain;

[TestFixture]
public class ReadinessCalculatorTests
{
    [Test]
    public void Calculate_SufficientMargin_CrewedWithCrew_ReturnsReady()
    {
        var result = ReadinessCalculator.Calculate(5500, 4500, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.Ready));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.ReserveMarginPercent, Is.GreaterThanOrEqualTo(10.0));
        });
    }

    [Test]
    public void Calculate_LowMargin_ReturnsAtRisk_WithLowReserveWarning()
    {
        // 4800 / 4500 = 6.67% margin
        var result = ReadinessCalculator.Calculate(4800, 4500, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.AtRisk));
            Assert.That(result.Warnings, Has.Exactly(1).Items);
            Assert.That(result.Warnings[0].Type, Is.EqualTo(WarningType.LowReserveMargin));
            Assert.That(result.Warnings[0].IsBlocking, Is.False);
        });
    }

    [Test]
    public void Calculate_ZeroMargin_ExactlyEqual_ReturnsAtRisk()
    {
        var result = ReadinessCalculator.Calculate(5000, 5000, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.AtRisk));
            Assert.That(result.ReserveMarginPercent, Is.EqualTo(0.0));
            Assert.That(result.Warnings, Has.Exactly(1).Items);
            Assert.That(result.Warnings[0].Type, Is.EqualTo(WarningType.LowReserveMargin));
        });
    }

    [Test]
    public void Calculate_InsufficientDeltaV_ReturnsNotReady()
    {
        var result = ReadinessCalculator.Calculate(4000, 4500, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.NotReady));
            Assert.That(result.Warnings.Any(w => w.Type == WarningType.InsufficientDeltaV), Is.True);
        });
    }

    [Test]
    public void Calculate_InsufficientDeltaV_AlsoFiresLowMargin()
    {
        var result = ReadinessCalculator.Calculate(4000, 4500, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.That(result.Warnings, Has.Exactly(2).Items);
        Assert.That(result.Warnings.Any(w => w.Type == WarningType.InsufficientDeltaV), Is.True);
        Assert.That(result.Warnings.Any(w => w.Type == WarningType.LowReserveMargin), Is.True);
    }

    [Test]
    public void Calculate_MissingCrew_ReturnsNotReady()
    {
        var result = ReadinessCalculator.Calculate(5500, 4500, MissionControlMode.Crewed, Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.NotReady));
            Assert.That(result.Warnings.Any(w => w.Type == WarningType.MissingCrew), Is.True);
        });
    }

    [Test]
    public void Calculate_NotReady_And_LowMargin_Simultaneous()
    {
        // Insufficient ΔV + no crew = two blocking + one low margin
        var result = ReadinessCalculator.Calculate(4000, 4500, MissionControlMode.Crewed, Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.NotReady));
            Assert.That(result.Warnings.Any(w => w.Type == WarningType.InsufficientDeltaV), Is.True);
            Assert.That(result.Warnings.Any(w => w.Type == WarningType.LowReserveMargin), Is.True);
            Assert.That(result.Warnings.Any(w => w.Type == WarningType.MissingCrew), Is.True);
        });
    }

    [Test]
    public void Calculate_ProbeMode_NoCrew_ReturnsReady()
    {
        var result = ReadinessCalculator.Calculate(5500, 4500, MissionControlMode.Probe, Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ReadinessState.Ready));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Calculate_Exact10PercentMargin_ReturnsReady()
    {
        // 4950 / 4500 = 10.0% exactly
        var result = ReadinessCalculator.Calculate(4950, 4500, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.That(result.State, Is.EqualTo(ReadinessState.Ready));
    }

    [Test]
    public void Calculate_JustBelow10PercentMargin_ReturnsAtRisk()
    {
        // 4949 / 4500 = 9.978% → below 10%
        var result = ReadinessCalculator.Calculate(4949, 4500, MissionControlMode.Crewed, new[] { "Jeb" });

        Assert.That(result.State, Is.EqualTo(ReadinessState.AtRisk));
    }
}
