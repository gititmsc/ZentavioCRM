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
            // A recurrence only makes sense with an anchor due date and at least 2 occurrences —
            // otherwise it's treated as a normal one-off activity (RecurrenceRule/GroupId left null).
            var isRecurring = request.RecurrenceRule is not null && request.DueAtUtc is not null && request.RecurrenceCount is >= 2;
            var recurrenceGroupId = isRecurring ? Guid.NewGuid() : (Guid?)null;

            var activity = new Activity
            {
                Type = request.Type,
                Subject = request.Subject.Trim(),
                Description = request.Description,
                RelatedToType = relatedToType,
                RelatedToId = relatedToId,
                DueAtUtc = request.DueAtUtc,
                AssignedToUserId = request.AssignedToUserId,
                RecurrenceRule = isRecurring ? request.RecurrenceRule : null,
                RecurrenceGroupId = recurrenceGroupId,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _activityRepository.AddAsync(activity);

            if (isRecurring)
            {
                // Every future occurrence is generated up front as a real Activity row — there's no
                // background job scheduler in this app, so this is how "recurring" activities work:
                // each occurrence is a normal row that the existing due-date reminder poll already covers.
                var occurrenceCount = Math.Min(request.RecurrenceCount!.Value, 52);
                for (var i = 1; i < occurrenceCount; i++)
                {
                    var occurrence = new Activity
                    {
                        Type = request.Type,
                        Subject = request.Subject.Trim(),
                        Description = request.Description,
                        RelatedToType = relatedToType,
                        RelatedToId = relatedToId,
                        DueAtUtc = NextOccurrenceDueDate(request.DueAtUtc!.Value, request.RecurrenceRule!.Value, i),
                        AssignedToUserId = request.AssignedToUserId,
                        RecurrenceRule = request.RecurrenceRule,
                        RecurrenceGroupId = recurrenceGroupId,
                        CreatedByUserId = currentUserId,
                        CreatedAtUtc = DateTime.UtcNow,
                    };

                    await _activityRepository.AddAsync(occurrence);
                }
            }

            return Map(activity);
        }

        private static DateTime NextOccurrenceDueDate(DateTime anchorDueAtUtc, ActivityRecurrenceRule rule, int stepsAhead) => rule switch
        {
            ActivityRecurrenceRule.Daily => anchorDueAtUtc.AddDays(stepsAhead),
            ActivityRecurrenceRule.Weekly => anchorDueAtUtc.AddDays(7 * stepsAhead),
            ActivityRecurrenceRule.Monthly => anchorDueAtUtc.AddMonths(stepsAhead),
            _ => anchorDueAtUtc,
        };

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
            RecurrenceRule = activity.RecurrenceRule,
            RecurrenceGroupId = activity.RecurrenceGroupId,
            CreatedAtUtc = activity.CreatedAtUtc,
        };
    }
}
