using ZentavioCRM.Core.DTOs.Activities;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IActivityService"/>
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _activityRepository;

        public ActivityService(IActivityRepository activityRepository)
        {
            _activityRepository = activityRepository;
        }

        public async Task<IReadOnlyList<ActivityDto>> GetTimelineAsync(RelatedEntityType relatedToType, Guid relatedToId)
        {
            var activities = await _activityRepository.GetTimelineAsync(relatedToType, relatedToId);
            return activities.Select(Map).ToList();
        }

        public async Task<ActivityDto> CreateAsync(RelatedEntityType relatedToType, Guid relatedToId, CreateActivityRequest request, Guid? currentUserId)
        {
            var activity = new Activity
            {
                Type = request.Type,
                Subject = request.Subject.Trim(),
                Description = request.Description,
                RelatedToType = relatedToType,
                RelatedToId = relatedToId,
                DueAtUtc = request.DueAtUtc,
                AssignedToUserId = request.AssignedToUserId,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _activityRepository.AddAsync(activity);

            return Map(activity);
        }

        private static ActivityDto Map(Activity activity) => new()
        {
            Id = activity.Id,
            Type = activity.Type,
            Subject = activity.Subject,
            Description = activity.Description,
            RelatedToType = activity.RelatedToType,
            RelatedToId = activity.RelatedToId,
            DueAtUtc = activity.DueAtUtc,
            CompletedAtUtc = activity.CompletedAtUtc,
            AssignedToUserId = activity.AssignedToUserId,
            AssignedToUserName = activity.AssignedToUser?.FullName,
            CreatedByUserName = activity.CreatedByUser?.FullName,
            CreatedAtUtc = activity.CreatedAtUtc,
        };
    }
}
