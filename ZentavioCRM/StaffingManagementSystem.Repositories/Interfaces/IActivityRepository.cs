using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IActivityRepository
    {
        Task<IReadOnlyList<Activity>> GetTimelineAsync(RelatedEntityType relatedToType, Guid relatedToId);

        Task AddAsync(Activity activity);
    }
}
