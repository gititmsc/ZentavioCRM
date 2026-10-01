using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Tags;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ITagService"/>
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<IReadOnlyList<TagDto>> GetAllAsync()
        {
            var tags = await _tagRepository.GetAllAsync();
            return tags.Select(Map).ToList();
        }

        public async Task<IReadOnlyList<TagWithUsageDto>> GetAllWithUsageAsync()
        {
            var tags = await _tagRepository.GetAllAsync();
            var result = new List<TagWithUsageDto>(tags.Count);

            foreach (var tag in tags)
            {
                var (leadCount, customerCount) = await _tagRepository.CountUsageAsync(tag.Id);
                result.Add(new TagWithUsageDto
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAtUtc = tag.CreatedAtUtc,
                    LeadCount = leadCount,
                    CustomerCount = customerCount,
                });
            }

            return result;
        }

        public async Task<ApiResponse<TagDto>> CreateAsync(SaveTagRequest request)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return ApiResponse<TagDto>.FailureResponse("Tag name is required.");
            }

            var existing = await _tagRepository.GetByNameAsync(name);
            if (existing is not null)
            {
                return ApiResponse<TagDto>.FailureResponse($"A tag named \"{existing.Name}\" already exists.");
            }

            var tag = new Tag
            {
                Name = name,
                Color = request.Color,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _tagRepository.AddAsync(tag);
            return ApiResponse<TagDto>.SuccessResponse(Map(tag), "Tag created.");
        }

        public async Task<ApiResponse<TagDto>> UpdateAsync(Guid id, SaveTagRequest request)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag is null)
            {
                return ApiResponse<TagDto>.FailureResponse("Tag not found.");
            }

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return ApiResponse<TagDto>.FailureResponse("Tag name is required.");
            }

            var existing = await _tagRepository.GetByNameAsync(name);
            if (existing is not null && existing.Id != id)
            {
                return ApiResponse<TagDto>.FailureResponse($"A tag named \"{existing.Name}\" already exists.");
            }

            tag.Name = name;
            tag.Color = request.Color;

            await _tagRepository.UpdateAsync(tag);
            return ApiResponse<TagDto>.SuccessResponse(Map(tag), "Tag updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag is null)
            {
                return ApiResponse<bool>.FailureResponse("Tag not found.");
            }

            await _tagRepository.DeleteAsync(tag);
            return ApiResponse<bool>.SuccessResponse(true, "Tag deleted.");
        }

        private static TagDto Map(Tag tag) => new()
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
            CreatedAtUtc = tag.CreatedAtUtc,
        };
    }
}
