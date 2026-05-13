using MissionControl.Domain.ValueObjects;

namespace MissionControl.Domain.Entities;

public sealed class Rocket
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? Notes { get; private set; }
    public IReadOnlyList<Stage> Stages { get; private set; } = Array.Empty<Stage>();
    public bool UsesAsparagusStaging { get; private set; }
    public double AsparagusEfficiencyBonus { get; private set; }

    private Rocket() { }

    public static Rocket Create(
        string name,
        string description,
        IReadOnlyList<Stage> stages,
        bool usesAsparagusStaging,
        double asparagusEfficiencyBonus,
        string? notes = null)
    {
        var rocket = new Rocket { Id = Guid.NewGuid() };
        rocket.Apply(name, description, stages, usesAsparagusStaging, asparagusEfficiencyBonus, notes);
        return rocket;
    }

    public void Update(
        string name,
        string description,
        IReadOnlyList<Stage> stages,
        bool usesAsparagusStaging,
        double asparagusEfficiencyBonus,
        string? notes = null)
    {
        Apply(name, description, stages, usesAsparagusStaging, asparagusEfficiencyBonus, notes);
    }

    internal static Rocket Reconstitute(
        Guid id,
        string name,
        string description,
        IReadOnlyList<Stage> stages,
        bool usesAsparagusStaging,
        double asparagusEfficiencyBonus,
        string? notes)
    {
        var rocket = new Rocket
        {
            Id = id,
            Name = name,
            Description = description,
            Stages = stages,
            UsesAsparagusStaging = usesAsparagusStaging,
            AsparagusEfficiencyBonus = asparagusEfficiencyBonus,
            Notes = notes
        };
        return rocket;
    }

    private void Apply(
        string name,
        string description,
        IReadOnlyList<Stage> stages,
        bool usesAsparagusStaging,
        double asparagusEfficiencyBonus,
        string? notes)
    {
        Validate(name, description, stages, asparagusEfficiencyBonus);

        Name = name;
        Description = description;
        Stages = stages;
        UsesAsparagusStaging = usesAsparagusStaging;
        AsparagusEfficiencyBonus = asparagusEfficiencyBonus;
        Notes = notes;
    }

    private static void Validate(
        string name,
        string description,
        IReadOnlyList<Stage> stages,
        double asparagusEfficiencyBonus)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Rocket name is required.");
        if (name.Length > 200)
            throw new DomainException("Rocket name cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Rocket description is required.");
        if (description.Length > 500)
            throw new DomainException("Rocket description cannot exceed 500 characters.");

        if (stages == null || stages.Count == 0)
            throw new DomainException("A rocket must have at least one stage.");

        var stageNumbers = stages.Select(s => s.StageNumber).ToList();
        if (stageNumbers.Distinct().Count() != stageNumbers.Count)
            throw new DomainException("Stage numbers must be unique within a rocket.");

        if (asparagusEfficiencyBonus < 0.0 || asparagusEfficiencyBonus > 0.20)
            throw new DomainException("Asparagus efficiency bonus must be between 0% and 20%.");
    }
}

public sealed class Stage
{
    public Guid Id { get; private set; }
    public int StageNumber { get; private set; }
    public string Name { get; private set; } = null!;
    public IReadOnlyList<StageEntry> Parts { get; private set; } = Array.Empty<StageEntry>();
    public bool IsJettisoned { get; private set; }
    public string? Notes { get; private set; }

    private Stage() { }

    public static Stage Create(
        int stageNumber,
        string name,
        IReadOnlyList<StageEntry> parts,
        bool isJettisoned = true,
        string? notes = null)
    {
        if (stageNumber < 1)
            throw new DomainException("Stage number must be at least 1.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Stage name is required.");
        if (name.Length > 100)
            throw new DomainException("Stage name cannot exceed 100 characters.");
        if (parts == null || parts.Count == 0)
            throw new DomainException("A stage must contain at least one part.");

        return new Stage
        {
            Id = Guid.NewGuid(),
            StageNumber = stageNumber,
            Name = name,
            Parts = parts,
            IsJettisoned = isJettisoned,
            Notes = notes
        };
    }

    internal static Stage Reconstitute(
        Guid id,
        int stageNumber,
        string name,
        IReadOnlyList<StageEntry> parts,
        bool isJettisoned,
        string? notes) => new()
    {
        Id = id,
        StageNumber = stageNumber,
        Name = name,
        Parts = parts,
        IsJettisoned = isJettisoned,
        Notes = notes
    };
}
