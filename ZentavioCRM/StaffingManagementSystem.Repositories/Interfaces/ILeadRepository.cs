using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ILeadRepository
    {
        Task<Lead?> GetByIdAsync(Guid id);

        Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize);

        Task<string> GetNextLeadNumberAsync();

        Task AddAsync(Lead lead);

        Task UpdateAsync(Lead lead);

        Task DeleteAsync(Lead lead);
    }
}
