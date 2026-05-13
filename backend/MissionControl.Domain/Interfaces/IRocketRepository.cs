using MissionControl.Domain.Entities;

namespace MissionControl.Domain.Interfaces;

public interface IRocketRepository
{
    Task<IReadOnlyList<Rocket>> GetAllAsync();
    Task<Rocket?> GetByIdAsync(Guid id);
    Task<Rocket?> GetByNameAsync(string name);
    Task AddAsync(Rocket rocket);
    Task UpdateAsync(Rocket rocket);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<Guid>> GetMissionIdsAssignedToRocketAsync(Guid rocketId);
}
