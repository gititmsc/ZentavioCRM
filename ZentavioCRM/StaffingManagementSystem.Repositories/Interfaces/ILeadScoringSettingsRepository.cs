using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ILeadScoringSettingsRepository
    {
        /// <summary>Returns the tenant's single settings row, creating it with hardcoded-formula-matching defaults if it's somehow missing (e.g. a tenant database provisioned before this feature existed, before 007_LeadScoringSettings.sql ran).</summary>
        Task<LeadScoringSettings> GetOrCreateAsync();

        Task UpdateAsync(LeadScoringSettings settings);
    }
}
