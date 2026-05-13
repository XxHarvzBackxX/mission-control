using MissionControl.Domain.Enums;
using MissionControl.Domain.Services;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Domain.Entities;

public class Mission
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public KspBodyValue TargetBody { get; private set; } = null!;
    public KspBodyValue MissionType { get; private set; } = null!;
    public double AvailableDeltaV { get; private set; }
    public double RequiredDeltaV { get; private set; }
    public MissionControlMode ControlMode { get; private set; }
    public IReadOnlyList<string> CrewMembers { get; private set; } = Array.Empty<string>();
    public KspBodyValue? ProbeCore { get; private set; }
    public KerbinTime? StartMissionTime { get; private set; }
    public KerbinTime? EndMissionTime { get; private set; }
    public ReadinessState ReadinessState { get; private set; }
    public IReadOnlyList<Warning> Warnings { get; private set; } = Array.Empty<Warning>();

    // Rocket assignment fields (US2)
    public Guid? AssignedRocketId { get; private set; }
    public string? RocketName { get; private set; }
    public MissionCalculationProfile? CalculationProfile { get; private set; }

    private Mission() { }

    /// <summary>
    /// Factory method that creates a new mission, validates all inputs, and evaluates readiness.
    /// </summary>
    public static Mission Create(
        string name,
        KspBodyValue targetBody,
        KspBodyValue missionType,
        double availableDeltaV,
        double requiredDeltaV,
        MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers,
        KspBodyValue? probeCore,
        KerbinTime? startMissionTime,
        KerbinTime? endMissionTime)
    {
        var mission = new Mission { Id = Guid.NewGuid() };
        mission.Apply(name, targetBody, missionType, availableDeltaV, requiredDeltaV,
            controlMode, crewMembers, probeCore, startMissionTime, endMissionTime);
        return mission;
    }

    /// <summary>
    /// Updates the mission with new parameters, re-validates, and re-evaluates readiness.
    /// </summary>
    public void Update(
        string name,
        KspBodyValue targetBody,
        KspBodyValue missionType,
        double availableDeltaV,
        double requiredDeltaV,
        MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers,
        KspBodyValue? probeCore,
        KerbinTime? startMissionTime,
        KerbinTime? endMissionTime)
    {
        Apply(name, targetBody, missionType, availableDeltaV, requiredDeltaV,
            controlMode, crewMembers, probeCore, startMissionTime, endMissionTime);
    }

    /// <summary>
    /// Reconstitutes a Mission from persisted data and re-derives readiness.
    /// Used by the repository layer only.
    /// </summary>
    internal static Mission Reconstitute(
        Guid id,
        string name,
        KspBodyValue targetBody,
        KspBodyValue missionType,
        double availableDeltaV,
        double requiredDeltaV,
        MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers,
        KspBodyValue? probeCore,
        KerbinTime? startMissionTime,
        KerbinTime? endMissionTime,
        Guid? assignedRocketId = null,
        string? rocketName = null,
        MissionCalculationProfile? calculationProfile = null)
    {
        var mission = new Mission { Id = id };
        mission.ApplyWithoutValidation(name, targetBody, missionType, availableDeltaV, requiredDeltaV,
            controlMode, crewMembers, probeCore, startMissionTime, endMissionTime);
        mission.AssignedRocketId = assignedRocketId;
        mission.RocketName = rocketName;
        mission.CalculationProfile = calculationProfile;
        mission.EvaluateReadiness();
        return mission;
    }

    /// <summary>
    /// Assigns or unassigns a rocket from this mission.
    /// Pass null to clear the assignment.
    /// </summary>
    public void AssignRocket(Guid? rocketId, string? rocketName, MissionCalculationProfile? profile)
    {
        AssignedRocketId = rocketId;
        RocketName = rocketName;
        CalculationProfile = profile;
    }

    private void Apply(
        string name,
        KspBodyValue targetBody,
        KspBodyValue missionType,
        double availableDeltaV,
        double requiredDeltaV,
        MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers,
        KspBodyValue? probeCore,
        KerbinTime? startMissionTime,
        KerbinTime? endMissionTime)
    {
        ValidateRequired(name, targetBody, missionType, availableDeltaV, requiredDeltaV);
        ValidateControlModeFields(controlMode, crewMembers, probeCore);
        ValidateTimeRange(startMissionTime, endMissionTime);

        ApplyWithoutValidation(name, targetBody, missionType, availableDeltaV, requiredDeltaV,
            controlMode, crewMembers, probeCore, startMissionTime, endMissionTime);
        EvaluateReadiness();
    }

    private void ApplyWithoutValidation(
        string name,
        KspBodyValue targetBody,
        KspBodyValue missionType,
        double availableDeltaV,
        double requiredDeltaV,
        MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers,
        KspBodyValue? probeCore,
        KerbinTime? startMissionTime,
        KerbinTime? endMissionTime)
    {
        Name = name;
        TargetBody = targetBody;
        MissionType = missionType;
        AvailableDeltaV = availableDeltaV;
        RequiredDeltaV = requiredDeltaV;
        ControlMode = controlMode;
        CrewMembers = crewMembers;
        ProbeCore = probeCore;
        StartMissionTime = startMissionTime;
        EndMissionTime = endMissionTime;
    }

    private void EvaluateReadiness()
    {
        var result = ReadinessCalculator.Calculate(AvailableDeltaV, RequiredDeltaV, ControlMode, CrewMembers);
        var allWarnings = new List<Warning>(result.Warnings);

        // Time warnings — evaluated by the aggregate, not the calculator
        if (EndMissionTime.HasValue && StartMissionTime.HasValue &&
            EndMissionTime.Value.TotalSeconds <= StartMissionTime.Value.TotalSeconds)
        {
            allWarnings.Add(new Warning(
                WarningType.InvalidTimeRange,
                "End Mission Time must be later than Start Mission Time.",
                IsBlocking: true));
        }
        else if (EndMissionTime.HasValue && !StartMissionTime.HasValue)
        {
            allWarnings.Add(new Warning(
                WarningType.AdvisoryEndTimeWithoutStart,
                "End Mission Time is set but Start Mission Time is not specified.",
                IsBlocking: false));
        }

        ReadinessState = allWarnings.Any(w => w.IsBlocking) ? ReadinessState.NotReady : result.State;
        Warnings = allWarnings;
    }

    private static void ValidateRequired(string name, KspBodyValue targetBody, KspBodyValue missionType,
        double availableDeltaV, double requiredDeltaV)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Mission name is required.");
        if (name.Length > 200)
            throw new DomainException("Mission name cannot exceed 200 characters.");

        targetBody.Validate(KspBodyValue.TargetBodies, "Target body");
        missionType.Validate(KspBodyValue.MissionTypes, "Mission type");

        if (availableDeltaV <= 0)
            throw new DomainException("Available delta-v must be greater than zero.");
        if (requiredDeltaV <= 0)
            throw new DomainException("Required delta-v must be greater than zero.");
    }

    private static void ValidateControlModeFields(MissionControlMode controlMode,
        IReadOnlyList<string> crewMembers, KspBodyValue? probeCore)
    {
        if (controlMode == MissionControlMode.Crewed)
        {
            if (crewMembers.Any(c => string.IsNullOrWhiteSpace(c)))
                throw new DomainException("Crew member names cannot be empty.");
        }
        else // Probe
        {
            if (probeCore == null)
                throw new DomainException("A probe core is required for a Probe mission.");
            probeCore.Validate(KspBodyValue.ProbeCores, "Probe core");
        }
    }

    private static void ValidateTimeRange(KerbinTime? start, KerbinTime? end)
    {
        if (end.HasValue && start.HasValue && end.Value.TotalSeconds <= start.Value.TotalSeconds)
            throw new DomainException("End Mission Time must be later than Start Mission Time.");
    }
}
