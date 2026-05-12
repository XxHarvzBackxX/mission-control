using MissionControl.Domain;
using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Tests.Domain;

[TestFixture]
public class MissionTests
{
    private static KspBodyValue Mun => new("Mun", false);
    private static KspBodyValue Landing => new("Landing", false);
    private static KspBodyValue Okto => new("Probodobodyne OKTO", false);

    [Test]
    public void Create_Ready_CrewedMission()
    {
        var mission = Mission.Create(
            "Mun Landing", Mun, Landing,
            5500, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.Ready));
            Assert.That(mission.Warnings, Is.Empty);
            Assert.That(mission.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void Create_AtRisk_LowMargin()
    {
        var mission = Mission.Create(
            "Mun Landing", Mun, Landing,
            4800, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.AtRisk));
            Assert.That(mission.Warnings.Any(w => w.Type == WarningType.LowReserveMargin), Is.True);
        });
    }

    [Test]
    public void Create_NotReady_InsufficientDeltaV()
    {
        var mission = Mission.Create(
            "Mun Landing", Mun, Landing,
            4000, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.NotReady));
            Assert.That(mission.Warnings.Any(w => w.Type == WarningType.InsufficientDeltaV), Is.True);
        });
    }

    [Test]
    public void Create_NotReady_MissingCrew()
    {
        var mission = Mission.Create(
            "Mun Landing", Mun, Landing,
            5500, 4500, MissionControlMode.Crewed,
            Array.Empty<string>(), null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.NotReady));
            Assert.That(mission.Warnings.Any(w => w.Type == WarningType.MissingCrew), Is.True);
        });
    }

    [Test]
    public void Create_NotReady_ProbeMode_NoCore()
    {
        Assert.That(() => Mission.Create(
            "Probe Test", Mun, Landing,
            5500, 4500, MissionControlMode.Probe,
            Array.Empty<string>(), null, null, null),
            Throws.TypeOf<DomainException>()
                .With.Message.Contains("probe core is required"));
    }

    [Test]
    public void Create_ProbeMode_WithCore_Ready()
    {
        var mission = Mission.Create(
            "Probe Test", Mun, Landing,
            5500, 4500, MissionControlMode.Probe,
            Array.Empty<string>(), Okto, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.Ready));
            Assert.That(mission.ControlMode, Is.EqualTo(MissionControlMode.Probe));
        });
    }

    [Test]
    public void Create_EndTimeBeforeStartTime_ThrowsDomainException()
    {
        Assert.That(() => Mission.Create(
            "Timed Mission", Mun, Landing,
            5500, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null,
            new KerbinTime(100000), new KerbinTime(50000)),
            Throws.TypeOf<DomainException>()
                .With.Message.Contains("End Mission Time must be later"));
    }

    [Test]
    public void Create_EndTimeWithoutStartTime_AdvisoryWarning()
    {
        var mission = Mission.Create(
            "Timed Mission", Mun, Landing,
            5500, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null,
            null, new KerbinTime(100000));

        Assert.Multiple(() =>
        {
            Assert.That(mission.Warnings.Any(w => w.Type == WarningType.AdvisoryEndTimeWithoutStart), Is.True);
            Assert.That(mission.Warnings.First(w => w.Type == WarningType.AdvisoryEndTimeWithoutStart).IsBlocking, Is.False);
            Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.Ready));
        });
    }

    [Test]
    public void Update_ReEvaluatesReadiness()
    {
        var mission = Mission.Create(
            "Mun Landing", Mun, Landing,
            5500, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null, null, null);

        Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.Ready));

        mission.Update(
            "Mun Landing", Mun, Landing,
            4000, 4500, MissionControlMode.Crewed,
            new[] { "Jebediah" }, null, null, null);

        Assert.That(mission.ReadinessState, Is.EqualTo(ReadinessState.NotReady));
    }
}
