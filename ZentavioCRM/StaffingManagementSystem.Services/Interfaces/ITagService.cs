using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Tags;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ITagService
    {
        Task<IReadOnlyList<TagDto>> GetAllAsync();

        /// <summary>Powers the Tag Manager list — same tags as <see cref="GetAllAsync"/>, with usage counts attached.</summary>
        Task<IReadOnlyList<TagWithUsageDto>> GetAllWithUsageAsync();

        Task<ApiResponse<TagDto>> CreateAsync(SaveTagRequest request);

        Task<ApiResponse<TagDto>> UpdateAsync(Guid id, SaveTagRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
