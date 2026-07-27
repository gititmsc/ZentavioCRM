using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="INotificationRepository"/>
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _dbContext;

        public NotificationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Notification notification)
        {
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Notification>> GetRecentForUserAsync(Guid userId, int take = 20)
        {
            var notifications = await _dbContext.Notifications
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(take)
                .ToListAsync();

            return notifications;
        }

        public Task<int> GetUnreadCountAsync(Guid userId)
            => _dbContext.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);

        public async Task<bool> MarkAsReadAsync(Guid id, Guid userId)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);
            if (notification is null)
            {
                return false;
            }

            notification.IsRead = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _dbContext.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
        }
    }
}
