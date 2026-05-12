using MissionControl.Domain.Entities;

namespace MissionControl.Domain.Interfaces;

public interface IMissionRepository
{
    Task<IReadOnlyList<Mission>> GetAllAsync();
    Task<Mission?> GetByIdAsync(Guid id);
    Task<Mission?> GetByNameAsync(string name);
    Task AddAsync(Mission mission);
    Task UpdateAsync(Mission mission);
    Task DeleteAsync(Guid id);
}
