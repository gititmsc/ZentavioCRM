using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IDocumentRepository"/>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _dbContext;

        public DocumentRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Document document)
        {
            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Document>> GetForEntityAsync(string entityType, Guid entityId)
            => await _dbContext.Documents
                .Include(d => d.UploadedByUser)
                .Where(d => d.EntityType == entityType && d.EntityId == entityId)
                .Select(d => new Document
                {
                    Id = d.Id,
                    EntityType = d.EntityType,
                    EntityId = d.EntityId,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    SizeBytes = d.SizeBytes,
                    UploadedByUserId = d.UploadedByUserId,
                    UploadedByUser = d.UploadedByUser,
                    CreatedAtUtc = d.CreatedAtUtc,
                    // Content intentionally omitted — listing must stay cheap.
                })
                .OrderByDescending(d => d.CreatedAtUtc)
                .ToListAsync();

        public Task<Document?> GetByIdAsync(Guid id)
            => _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id);

        public async Task DeleteAsync(Document document)
        {
            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync();
        }
    }
}
