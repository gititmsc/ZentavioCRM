using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IDocumentRepository
    {
        Task AddAsync(Document document);

        /// <summary>Metadata only (no Content bytes) — cheap to list.</summary>
        Task<IReadOnlyList<Document>> GetForEntityAsync(string entityType, Guid entityId);

        /// <summary>Includes Content bytes — for download.</summary>
        Task<Document?> GetByIdAsync(Guid id);

        Task DeleteAsync(Document document);
    }
}
