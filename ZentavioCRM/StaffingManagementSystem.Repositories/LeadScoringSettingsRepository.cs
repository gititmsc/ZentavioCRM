using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ILeadScoringSettingsRepository"/>
    public class LeadScoringSettingsRepository : ILeadScoringSettingsRepository
    {
        private readonly AppDbContext _dbContext;

        public LeadScoringSettingsRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<LeadScoringSettings> GetOrCreateAsync()
        {
            var settings = await _dbContext.LeadScoringSettings.FirstOrDefaultAsync();
            if (settings is not null)
            {
                return settings;
            }

            // Lazily create the singleton row with defaults — only reachable for a tenant database
            // provisioned before this feature existed, since TenantSchema.sql now seeds this row
            // for every newly-provisioned tenant.
            settings = new LeadScoringSettings { Id = SeedIds.LeadScoringSettingsId };
            _dbContext.LeadScoringSettings.Add(settings);
            await _dbContext.SaveChangesAsync();
            return settings;
        }

        public async Task UpdateAsync(LeadScoringSettings settings)
        {
            _dbContext.LeadScoringSettings.Update(settings);
            await _dbContext.SaveChangesAsync();
        }
    }
}
