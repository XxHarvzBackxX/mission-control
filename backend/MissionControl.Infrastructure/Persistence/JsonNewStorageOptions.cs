namespace MissionControl.Infrastructure.Persistence;

public class JsonRocketStorageOptions
{
    public string FilePath { get; set; } = "data/rockets.json";
}

public class JsonPartCatalogueStorageOptions
{
    public string FilePath { get; set; } = "data/parts.json";
    /// <summary>Baseline seed file used to initialise FilePath when it is missing.</summary>
    public string SeedFilePath { get; set; } = "data/seed/parts.json";
}

public class JsonCelestialBodyStorageOptions
{
    public string FilePath { get; set; } = "data/celestial-bodies.json";
    /// <summary>Baseline seed file used to initialise FilePath when it is missing.</summary>
    public string SeedFilePath { get; set; } = "data/seed/celestial-bodies.json";
}
