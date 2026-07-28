using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IActivityRepository
    {
        Task<IReadOnlyList<Activity>> GetTimelineAsync(RelatedEntityType relatedToType, Guid relatedToId);

        Task AddAsync(Activity activity);

        /// <summary>Open (uncompleted), overdue activities assigned to the user that haven't had a reminder sent yet.</summary>
        Task<IReadOnlyList<Activity>> GetDueForReminderAsync(Guid userId, DateTime nowUtc);

        Task UpdateAsync(Activity activity);
    }
}
