using MissionControl.Domain.Entities;

namespace MissionControl.Domain.Interfaces;

public interface ICelestialBodyRepository
{
    Task<IReadOnlyList<CelestialBody>> GetAllAsync();
    Task<CelestialBody?> GetByIdAsync(string id);
    Task AddCustomAsync(CelestialBody body);
}
