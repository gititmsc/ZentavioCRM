using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IActivityRepository"/>
    public class ActivityRepository : IActivityRepository
    {
        private readonly AppDbContext _dbContext;

        public ActivityRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Activity>> GetTimelineAsync(RelatedEntityType relatedToType, Guid relatedToId)
            => await _dbContext.Activities
                .Include(a => a.AssignedToUser)
                .Include(a => a.CreatedByUser)
                .Where(a => a.RelatedToType == relatedToType && a.RelatedToId == relatedToId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync();

        public async Task AddAsync(Activity activity)
        {
            _dbContext.Activities.Add(activity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Activity>> GetDueForReminderAsync(Guid userId, DateTime nowUtc)
            => await _dbContext.Activities
                .Where(a =>
                    a.AssignedToUserId == userId &&
                    a.CompletedAtUtc == null &&
                    a.ReminderSentAtUtc == null &&
                    a.DueAtUtc != null && a.DueAtUtc <= nowUtc)
                .ToListAsync();

        public async Task UpdateAsync(Activity activity)
        {
            _dbContext.Activities.Update(activity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
