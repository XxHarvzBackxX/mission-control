using MissionControl.Domain.Entities;
using MissionControl.Domain.Enums;

namespace MissionControl.Domain.Interfaces;

public interface IPartCatalogueRepository
{
    Task<IReadOnlyList<CataloguePart>> GetAllAsync();
    Task<CataloguePart?> GetByIdAsync(string id);
    Task<IReadOnlyList<CataloguePart>> GetByCategoryAsync(PartCategory category);
    Task<IReadOnlyList<CataloguePart>> SearchByNameAsync(string query);
}
