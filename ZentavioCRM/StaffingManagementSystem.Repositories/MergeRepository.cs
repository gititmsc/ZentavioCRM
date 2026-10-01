using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IMergeRepository"/>
    public class MergeRepository : IMergeRepository
    {
        private readonly AppDbContext _dbContext;

        public MergeRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MergeResultDto> MergeLeadsAsync(Guid survivingLeadId, Guid losingLeadId)
        {
            if (survivingLeadId == losingLeadId)
            {
                throw new InvalidOperationException("Cannot merge a lead into itself.");
            }

            var surviving = await _dbContext.Leads.FirstOrDefaultAsync(l => l.Id == survivingLeadId)
                ?? throw new InvalidOperationException("Surviving lead not found.");
            var losing = await _dbContext.Leads.FirstOrDefaultAsync(l => l.Id == losingLeadId)
                ?? throw new InvalidOperationException("Losing lead not found.");

            var result = new MergeResultDto { SurvivingId = survivingLeadId, LosingId = losingLeadId };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var activities = await _dbContext.Activities
                .Where(a => a.RelatedToType == RelatedEntityType.Lead && a.RelatedToId == losingLeadId)
                .ToListAsync();
            foreach (var activity in activities)
            {
                activity.RelatedToId = survivingLeadId;
            }
            result.ActivitiesMoved = activities.Count;

            var opportunities = await _dbContext.Opportunities
                .Where(o => o.SourceLeadId == losingLeadId)
                .ToListAsync();
            foreach (var opportunity in opportunities)
            {
                opportunity.SourceLeadId = survivingLeadId;
            }
            result.OpportunitiesMoved = opportunities.Count;

            var survivingTagIds = (await _dbContext.LeadTags
                .Where(lt => lt.LeadId == survivingLeadId)
                .Select(lt => lt.TagId)
                .ToListAsync())
                .ToHashSet();
            var losingTags = await _dbContext.LeadTags.Where(lt => lt.LeadId == losingLeadId).ToListAsync();
            var tagsToAdd = losingTags.Where(lt => !survivingTagIds.Contains(lt.TagId)).ToList();
            _dbContext.LeadTags.AddRange(tagsToAdd.Select(lt => new LeadTag { LeadId = survivingLeadId, TagId = lt.TagId }));
            result.TagsMerged = tagsToAdd.Count;
            _dbContext.LeadTags.RemoveRange(losingTags);

            await _dbContext.SaveChangesAsync();

            // All FK references to the losing lead have now been repointed or removed above, so it
            // can be safely deleted — Lead has no soft-delete/archive flag (unlike Customer.IsActive).
            _dbContext.Leads.Remove(losing);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            result.LosingRecordArchived = false;
            return result;
        }

        public async Task<MergeResultDto> MergeCustomersAsync(Guid survivingCustomerId, Guid losingCustomerId)
        {
            if (survivingCustomerId == losingCustomerId)
            {
                throw new InvalidOperationException("Cannot merge a customer into itself.");
            }

            var surviving = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == survivingCustomerId)
                ?? throw new InvalidOperationException("Surviving customer not found.");
            var losing = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == losingCustomerId)
                ?? throw new InvalidOperationException("Losing customer not found.");

            var result = new MergeResultDto { SurvivingId = survivingCustomerId, LosingId = losingCustomerId };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var activities = await _dbContext.Activities
                .Where(a => a.RelatedToType == RelatedEntityType.Customer && a.RelatedToId == losingCustomerId)
                .ToListAsync();
            foreach (var activity in activities)
            {
                activity.RelatedToId = survivingCustomerId;
            }
            result.ActivitiesMoved = activities.Count;

            var opportunities = await _dbContext.Opportunities.Where(o => o.CustomerId == losingCustomerId).ToListAsync();
            foreach (var opportunity in opportunities)
            {
                opportunity.CustomerId = survivingCustomerId;
            }
            result.OpportunitiesMoved = opportunities.Count;

            var quotations = await _dbContext.Quotations.Where(q => q.CustomerId == losingCustomerId).ToListAsync();
            foreach (var quotation in quotations)
            {
                quotation.CustomerId = survivingCustomerId;
            }
            result.QuotationsMoved = quotations.Count;

            var salesOrders = await _dbContext.SalesOrders.Where(so => so.CustomerId == losingCustomerId).ToListAsync();
            foreach (var salesOrder in salesOrders)
            {
                salesOrder.CustomerId = survivingCustomerId;
            }
            result.SalesOrdersMoved = salesOrders.Count;

            var contacts = await _dbContext.ContactPersons.Where(cp => cp.CustomerId == losingCustomerId).ToListAsync();
            foreach (var contact in contacts)
            {
                contact.CustomerId = survivingCustomerId;
                // Avoid colliding with the survivor's own primary contact — the user can re-pick a
                // primary afterward if they want one of the moved contacts to take that role.
                contact.IsPrimary = false;
            }
            result.ContactsMoved = contacts.Count;

            var addresses = await _dbContext.CustomerAddresses.Where(ca => ca.CustomerId == losingCustomerId).ToListAsync();
            foreach (var address in addresses)
            {
                address.CustomerId = survivingCustomerId;
                address.IsPrimary = false;
            }
            result.AddressesMoved = addresses.Count;

            var leadsLinkedToLosing = await _dbContext.Leads.Where(l => l.LinkedCustomerId == losingCustomerId).ToListAsync();
            foreach (var lead in leadsLinkedToLosing)
            {
                lead.LinkedCustomerId = survivingCustomerId;
            }

            var leadsConvertedToLosing = await _dbContext.Leads.Where(l => l.ConvertedCustomerId == losingCustomerId).ToListAsync();
            foreach (var lead in leadsConvertedToLosing)
            {
                lead.ConvertedCustomerId = survivingCustomerId;
            }

            var survivingTagIds = (await _dbContext.CustomerTags
                .Where(ct => ct.CustomerId == survivingCustomerId)
                .Select(ct => ct.TagId)
                .ToListAsync())
                .ToHashSet();
            var losingTags = await _dbContext.CustomerTags.Where(ct => ct.CustomerId == losingCustomerId).ToListAsync();
            var tagsToAdd = losingTags.Where(ct => !survivingTagIds.Contains(ct.TagId)).ToList();
            _dbContext.CustomerTags.AddRange(tagsToAdd.Select(ct => new CustomerTag { CustomerId = survivingCustomerId, TagId = ct.TagId }));
            result.TagsMerged = tagsToAdd.Count;
            _dbContext.CustomerTags.RemoveRange(losingTags);

            await _dbContext.SaveChangesAsync();

            // Archive rather than delete: a Customer can plausibly be referenced by records this
            // merge doesn't enumerate (future modules), so retiring it in place — same IsActive=false
            // convention already used for any other inactive customer — is safer than a hard delete.
            losing.IsActive = false;
            losing.DisplayName = $"{losing.DisplayName} (merged into {surviving.DisplayName})";
            losing.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            result.LosingRecordArchived = true;
            return result;
        }
    }
}
