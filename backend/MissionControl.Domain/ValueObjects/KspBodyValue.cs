namespace MissionControl.Domain.ValueObjects;

public sealed record KspBodyValue(string Value, bool IsCustom)
{
    public static readonly IReadOnlyList<string> TargetBodies = new[]
    {
        "Kerbol", "Moho", "Eve", "Gilly", "Kerbin", "Mun", "Minmus",
        "Duna", "Ike", "Dres", "Jool", "Laythe", "Vall", "Tylo",
        "Bop", "Pol", "Eeloo"
    };

    public static readonly IReadOnlyList<string> MissionTypes = new[]
    {
        "Orbital", "Landing", "Flyby", "Transfer", "Rescue",
        "Station Resupply", "Return"
    };

    public static readonly IReadOnlyList<string> ProbeCores = new[]
    {
        "Stayputnik",
        "Probodobodyne OKTO",
        "Probodobodyne HECS",
        "Probodobodyne QBE",
        "Probodobodyne OKTO2",
        "Probodobodyne HECS2",
        "Probodobodyne RoveMate",
        "RC-001S Remote Guidance Unit",
        "RC-L01 Remote Guidance Unit",
        "MK2 Drone Core"
    };

    public void Validate(IReadOnlyList<string> predefinedList, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new DomainException($"{fieldName} value is required.");

        if (!IsCustom && !predefinedList.Contains(Value))
            throw new DomainException($"'{Value}' is not a valid {fieldName}. Use a predefined value or mark as custom.");
    }
}
