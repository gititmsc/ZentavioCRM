using ZentavioCRM.Core.DTOs.Activities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IActivityService
    {
        Task<IReadOnlyList<ActivityDto>> GetTimelineAsync(RelatedEntityType relatedToType, Guid relatedToId);

        Task<ActivityDto> CreateAsync(RelatedEntityType relatedToType, Guid relatedToId, CreateActivityRequest request, Guid? currentUserId);
    }
}
