using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ITagRepository"/>
    public class TagRepository : ITagRepository
    {
        private readonly AppDbContext _dbContext;

        public TagRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Tag>> GetAllAsync()
            => await _dbContext.Tags.OrderBy(t => t.Name).ToListAsync();

        public Task<Tag?> GetByIdAsync(Guid id)
            => _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);

        public Task<Tag?> GetByNameAsync(string name)
            => _dbContext.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

        public async Task AddAsync(Tag tag)
        {
            _dbContext.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tag tag)
        {
            _dbContext.Tags.Update(tag);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Tag tag)
        {
            // LeadTags/CustomerTags rows cascade-delete at the database level (see TagConfiguration's
            // FK setup) — no need to remove them here first.
            _dbContext.Tags.Remove(tag);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<(int LeadCount, int CustomerCount)> CountUsageAsync(Guid tagId)
        {
            var leadCount = await _dbContext.LeadTags.CountAsync(lt => lt.TagId == tagId);
            var customerCount = await _dbContext.CustomerTags.CountAsync(ct => ct.TagId == tagId);
            return (leadCount, customerCount);
        }

        public async Task<IReadOnlyList<Tag>> GetForLeadAsync(Guid leadId)
            => await _dbContext.LeadTags
                .Where(lt => lt.LeadId == leadId)
                .Select(lt => lt.Tag!)
                .OrderBy(t => t.Name)
                .ToListAsync();

        public async Task<IReadOnlyList<Tag>> GetForCustomerAsync(Guid customerId)
            => await _dbContext.CustomerTags
                .Where(ct => ct.CustomerId == customerId)
                .Select(ct => ct.Tag!)
                .OrderBy(t => t.Name)
                .ToListAsync();

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Tag>>> GetForLeadsAsync(IReadOnlyCollection<Guid> leadIds)
        {
            if (leadIds.Count == 0)
            {
                return new Dictionary<Guid, IReadOnlyList<Tag>>();
            }

            var rows = await _dbContext.LeadTags
                .Where(lt => leadIds.Contains(lt.LeadId))
                .Select(lt => new { lt.LeadId, Tag = lt.Tag! })
                .ToListAsync();

            return rows
                .GroupBy(r => r.LeadId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Tag>)g.Select(r => r.Tag).OrderBy(t => t.Name).ToList());
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Tag>>> GetForCustomersAsync(IReadOnlyCollection<Guid> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new Dictionary<Guid, IReadOnlyList<Tag>>();
            }

            var rows = await _dbContext.CustomerTags
                .Where(ct => customerIds.Contains(ct.CustomerId))
                .Select(ct => new { ct.CustomerId, Tag = ct.Tag! })
                .ToListAsync();

            return rows
                .GroupBy(r => r.CustomerId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Tag>)g.Select(r => r.Tag).OrderBy(t => t.Name).ToList());
        }

        public async Task ReplaceLeadTagsAsync(Guid leadId, IReadOnlyCollection<Guid> tagIds)
        {
            var existing = await _dbContext.LeadTags.Where(lt => lt.LeadId == leadId).ToListAsync();
            var existingTagIds = existing.Select(lt => lt.TagId).ToHashSet();
            var requestedTagIds = tagIds.ToHashSet();

            var toRemove = existing.Where(lt => !requestedTagIds.Contains(lt.TagId));
            _dbContext.LeadTags.RemoveRange(toRemove);

            var toAdd = requestedTagIds.Where(id => !existingTagIds.Contains(id))
                .Select(id => new LeadTag { LeadId = leadId, TagId = id });
            _dbContext.LeadTags.AddRange(toAdd);

            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceCustomerTagsAsync(Guid customerId, IReadOnlyCollection<Guid> tagIds)
        {
            var existing = await _dbContext.CustomerTags.Where(ct => ct.CustomerId == customerId).ToListAsync();
            var existingTagIds = existing.Select(ct => ct.TagId).ToHashSet();
            var requestedTagIds = tagIds.ToHashSet();

            var toRemove = existing.Where(ct => !requestedTagIds.Contains(ct.TagId));
            _dbContext.CustomerTags.RemoveRange(toRemove);

            var toAdd = requestedTagIds.Where(id => !existingTagIds.Contains(id))
                .Select(id => new CustomerTag { CustomerId = customerId, TagId = id });
            _dbContext.CustomerTags.AddRange(toAdd);

            await _dbContext.SaveChangesAsync();
        }
    }
}
