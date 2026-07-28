using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ILeadService"/>
    public class LeadService : ILeadService
    {
        private const string EntityType = "Lead";

        private static readonly HashSet<LeadStatus> TerminalStatuses = [LeadStatus.Converted, LeadStatus.Lost, LeadStatus.Junk];

        private readonly ILeadRepository _leadRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IOpportunityRepository _opportunityRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public LeadService(
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            IOpportunityRepository opportunityRepository,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _opportunityRepository = opportunityRepository;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<LeadListItemDto>> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _leadRepository.SearchAsync(search, status, assignedToUserId, page, pageSize);

            return new PagedResult<LeadListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<LeadDto>> GetByIdAsync(Guid id)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            return lead is null
                ? ApiResponse<LeadDto>.FailureResponse("Lead not found.")
                : ApiResponse<LeadDto>.SuccessResponse(Map(lead));
        }

        public async Task<ApiResponse<LeadDto>> CreateAsync(SaveLeadRequest request, Guid? currentUserId)
        {
            var lead = new Lead
            {
                LeadNumber = await _leadRepository.GetNextLeadNumberAsync(),
                CompanyName = request.CompanyName.Trim(),
                ContactName = request.ContactName.Trim(),
                Email = request.Email,
                Mobile = request.Mobile,
                Industry = request.Industry,
                Source = request.Source,
                Campaign = request.Campaign,
                Budget = request.Budget,
                Timeline = request.Timeline,
                ExpectedValue = request.ExpectedValue,
                AssignedToUserId = request.AssignedToUserId,
                Territory = request.Territory,
                Status = request.AssignedToUserId is null ? LeadStatus.New : LeadStatus.Assigned,
                Notes = request.Notes,
                NextFollowUpDate = request.NextFollowUpDate,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            lead.LeadScore = ComputeLeadScore(lead);

            await _leadRepository.AddAsync(lead);
            await _auditLogService.LogAsync(EntityType, lead.Id, "Created", $"Lead {lead.LeadNumber} created.", currentUserId);

            var created = await _leadRepository.GetByIdAsync(lead.Id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(created!), "Lead created.");
        }

        /// <summary>
        /// Simple rule-based score (0-100) reflecting how "sales-ready" a lead looks, based only on
        /// fields already on the record — no external data or AI model. Recomputed on every save.
        /// A real scoring model (weighted by conversion-rate history, ideally ML-driven per the SRS's
        /// "AI Lead Intelligence" section) is a natural next iteration once there's enough historical
        /// conversion data to train against.
        /// </summary>
        private static int ComputeLeadScore(Lead lead)
        {
            var score = 0;

            if (!string.IsNullOrWhiteSpace(lead.Email)) score += 15;
            if (!string.IsNullOrWhiteSpace(lead.Mobile)) score += 15;
            if (!string.IsNullOrWhiteSpace(lead.Industry)) score += 10;
            if (lead.AssignedToUserId is not null) score += 10;

            if (lead.ExpectedValue is >= 50000) score += 25;
            else if (lead.ExpectedValue is >= 10000) score += 15;
            else if (lead.ExpectedValue is > 0) score += 5;

            score += lead.Source switch
            {
                LeadSource.Referral => 20,
                LeadSource.LinkedIn => 10,
                LeadSource.Website or LeadSource.LandingPage => 10,
                LeadSource.Exhibition => 10,
                _ => 0,
            };

            if (!string.IsNullOrWhiteSpace(lead.Timeline))
            {
                var timeline = lead.Timeline.ToLowerInvariant();
                if (timeline.Contains("month") || timeline.Contains("quarter") || timeline.Contains("immediate") || timeline.Contains("asap"))
                {
                    score += 5;
                }
            }

            return Math.Min(score, 100);
        }

        public async Task<ApiResponse<LeadDto>> UpdateAsync(Guid id, SaveLeadRequest request, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<LeadDto>.FailureResponse("Lead not found.");
            }

            if (TerminalStatuses.Contains(lead.Status))
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    $"This lead is {lead.Status} and can no longer be edited.",
                    ["Reopen the lead before editing it."]);
            }

            lead.CompanyName = request.CompanyName.Trim();
            lead.ContactName = request.ContactName.Trim();
            lead.Email = request.Email;
            lead.Mobile = request.Mobile;
            lead.Industry = request.Industry;
            lead.Source = request.Source;
            lead.Campaign = request.Campaign;
            lead.Budget = request.Budget;
            lead.Timeline = request.Timeline;
            lead.ExpectedValue = request.ExpectedValue;
            lead.Territory = request.Territory;
            lead.Notes = request.Notes;

            if (lead.NextFollowUpDate != request.NextFollowUpDate)
            {
                // A changed (or newly set) follow-up date means any previously-sent reminder no
                // longer applies — clear it so GetDueForFollowUpReminderAsync can fire again.
                lead.FollowUpReminderSentAtUtc = null;
            }
            lead.NextFollowUpDate = request.NextFollowUpDate;

            lead.UpdatedAtUtc = DateTime.UtcNow;
            lead.LeadScore = ComputeLeadScore(lead);

            // AssignedToUserId is changed exclusively through AssignAsync, which also drives the status transition.
            await _leadRepository.UpdateAsync(lead);
            await _auditLogService.LogAsync(EntityType, id, "Updated", "Lead details updated.", currentUserId);

            var updated = await _leadRepository.GetByIdAsync(id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(updated!), "Lead updated.");
        }

        public async Task<ApiResponse<LeadDto>> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<LeadDto>.FailureResponse("Lead not found.");
            }

            if (lead.Status == LeadStatus.Converted)
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    "A converted lead's status cannot be changed.",
                    ["This lead has already been converted to a customer."]);
            }

            if (request.Status is LeadStatus.Lost or LeadStatus.Junk && string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    "A reason is required when marking a lead as Lost or Junk.",
                    ["Reason is required."]);
            }

            if (request.Status == LeadStatus.Converted)
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    "Use the Convert action to move a lead to Converted — it also creates the customer record.",
                    ["Use POST /api/leads/{id}/convert instead."]);
            }

            var oldStatus = lead.Status;
            lead.Status = request.Status;
            lead.LostReason = request.Status is LeadStatus.Lost or LeadStatus.Junk ? request.Reason : null;
            lead.UpdatedAtUtc = DateTime.UtcNow;

            await _leadRepository.UpdateAsync(lead);
            await _auditLogService.LogAsync(EntityType, id, "StatusChanged", $"Status changed from {oldStatus} to {request.Status}.", currentUserId);

            var updated = await _leadRepository.GetByIdAsync(id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(updated!), "Lead status updated.");
        }

        public async Task<ApiResponse<LeadDto>> AssignAsync(Guid id, AssignLeadRequest request, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<LeadDto>.FailureResponse("Lead not found.");
            }

            if (TerminalStatuses.Contains(lead.Status))
            {
                return ApiResponse<LeadDto>.FailureResponse($"This lead is {lead.Status} and can no longer be reassigned.");
            }

            lead.AssignedToUserId = request.UserId;
            if (lead.Status == LeadStatus.New)
            {
                lead.Status = LeadStatus.Assigned;
            }
            lead.UpdatedAtUtc = DateTime.UtcNow;

            await _leadRepository.UpdateAsync(lead);
            await _auditLogService.LogAsync(EntityType, id, "Assigned", $"Lead {lead.LeadNumber} assigned.", currentUserId);

            var updated = await _leadRepository.GetByIdAsync(id);
            await _notificationService.NotifyAsync(
                request.UserId,
                $"You were assigned lead {updated!.LeadNumber} — {updated.CompanyName}.",
                RelatedEntityType.Lead,
                updated.Id);

            return ApiResponse<LeadDto>.SuccessResponse(Map(updated), "Lead assigned.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<bool>.FailureResponse("Lead not found.");
            }

            await _leadRepository.DeleteAsync(lead);
            await _auditLogService.LogAsync(EntityType, id, "Deleted", $"Lead {lead.LeadNumber} deleted.", currentUserId);
            return ApiResponse<bool>.SuccessResponse(true, "Lead deleted.");
        }

        public async Task<DuplicateCheckResultDto> CheckDuplicatesAsync(string? email, string? mobile, Guid? excludeLeadId)
        {
            var matchingLeads = await _leadRepository.FindPotentialDuplicatesAsync(email, mobile, excludeLeadId);
            var matchingCustomers = await _customerRepository.FindByEmailOrPhoneAsync(email, mobile);

            var matches = new List<DuplicateMatchDto>();
            matches.AddRange(matchingLeads.Select(l => new DuplicateMatchDto
            {
                Type = "Lead",
                Id = l.Id,
                Name = $"{l.CompanyName} ({l.ContactName})",
                Email = l.Email,
                Mobile = l.Mobile,
            }));
            matches.AddRange(matchingCustomers.Select(c => new DuplicateMatchDto
            {
                Type = "Customer",
                Id = c.Id,
                Name = c.DisplayName,
                Email = c.Email,
                Mobile = c.Phone,
            }));

            return new DuplicateCheckResultDto { Matches = matches };
        }

        private static readonly string[] ExportHeaders =
        [
            "LeadNumber", "CompanyName", "ContactName", "Email", "Mobile", "Industry", "Source",
            "Campaign", "Budget", "Timeline", "ExpectedValue", "Territory", "Status", "LeadScore",
            "AssignedToUserName", "Notes", "CreatedAtUtc",
        ];

        /// <summary>Columns accepted on import — a subset of the export columns: system-managed fields (LeadNumber, Status, LeadScore, AssignedToUserName, CreatedAtUtc) are not importable.</summary>
        private static readonly string[] ImportHeaders =
        [
            "CompanyName", "ContactName", "Email", "Mobile", "Industry", "Source",
            "Campaign", "Budget", "Timeline", "ExpectedValue", "Territory", "Notes",
        ];

        public async Task<string> ExportCsvAsync()
        {
            var leads = await _leadRepository.GetAllAsync();

            var rows = leads.Select(l => (IReadOnlyList<string?>)new List<string?>
            {
                l.LeadNumber,
                l.CompanyName,
                l.ContactName,
                l.Email,
                l.Mobile,
                l.Industry,
                l.Source.ToString(),
                l.Campaign,
                l.Budget?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                l.Timeline,
                l.ExpectedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                l.Territory,
                l.Status.ToString(),
                l.LeadScore?.ToString(),
                l.AssignedToUser?.FullName,
                l.Notes,
                l.CreatedAtUtc.ToString("O"),
            });

            return CsvUtility.Write(ExportHeaders, rows);
        }

        public async Task<ImportResultDto> ImportCsvAsync(string csvContent, Guid? currentUserId)
        {
            var (headers, rows) = CsvUtility.Parse(csvContent);
            var result = new ImportResultDto { TotalRows = rows.Count };

            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                columnIndex[headers[i].Trim()] = i;
            }

            var missingRequired = ImportHeaders
                .Where(h => h is "CompanyName" or "ContactName")
                .Where(h => !columnIndex.ContainsKey(h))
                .ToList();
            if (missingRequired.Count > 0)
            {
                result.Errors.Add(new ImportRowErrorDto { RowNumber = 0, Message = $"CSV is missing required column(s): {string.Join(", ", missingRequired)}." });
                return result;
            }

            string? Get(string[] row, string column)
                => columnIndex.TryGetValue(column, out var idx) && idx < row.Length && !string.IsNullOrWhiteSpace(row[idx]) ? row[idx].Trim() : null;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 1;
                var row = rows[i];

                try
                {
                    var companyName = Get(row, "CompanyName");
                    var contactName = Get(row, "ContactName");
                    if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(contactName))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = "CompanyName and ContactName are required." });
                        result.FailureCount++;
                        continue;
                    }

                    var sourceRaw = Get(row, "Source");
                    var source = sourceRaw is not null && Enum.TryParse<LeadSource>(sourceRaw, true, out var parsedSource)
                        ? parsedSource
                        : LeadSource.ManualEntry;

                    decimal? budget = null;
                    var budgetRaw = Get(row, "Budget");
                    if (budgetRaw is not null && !decimal.TryParse(budgetRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedBudget))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = $"Budget '{budgetRaw}' is not a valid number." });
                        result.FailureCount++;
                        continue;
                    }
                    else if (budgetRaw is not null)
                    {
                        budget = decimal.Parse(budgetRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    decimal? expectedValue = null;
                    var expectedValueRaw = Get(row, "ExpectedValue");
                    if (expectedValueRaw is not null && !decimal.TryParse(expectedValueRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedExpectedValue))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = $"ExpectedValue '{expectedValueRaw}' is not a valid number." });
                        result.FailureCount++;
                        continue;
                    }
                    else if (expectedValueRaw is not null)
                    {
                        expectedValue = decimal.Parse(expectedValueRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    var lead = new Lead
                    {
                        LeadNumber = await _leadRepository.GetNextLeadNumberAsync(),
                        CompanyName = companyName.Trim(),
                        ContactName = contactName.Trim(),
                        Email = Get(row, "Email"),
                        Mobile = Get(row, "Mobile"),
                        Industry = Get(row, "Industry"),
                        Source = source,
                        Campaign = Get(row, "Campaign"),
                        Budget = budget,
                        Timeline = Get(row, "Timeline"),
                        ExpectedValue = expectedValue,
                        Territory = Get(row, "Territory"),
                        Status = LeadStatus.New,
                        Notes = Get(row, "Notes"),
                        CreatedByUserId = currentUserId,
                        CreatedAtUtc = DateTime.UtcNow,
                    };
                    lead.LeadScore = ComputeLeadScore(lead);

                    await _leadRepository.AddAsync(lead);
                    await _auditLogService.LogAsync(EntityType, lead.Id, "Created", $"Lead {lead.LeadNumber} created via CSV import.", currentUserId);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = ex.Message });
                    result.FailureCount++;
                }
            }

            return result;
        }

        public async Task<ApiResponse<ConvertLeadResultDto>> ConvertAsync(Guid id, ConvertLeadRequest request, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<ConvertLeadResultDto>.FailureResponse("Lead not found.");
            }

            if (lead.Status == LeadStatus.Converted)
            {
                return ApiResponse<ConvertLeadResultDto>.FailureResponse("This lead has already been converted.");
            }

            if (lead.Status is LeadStatus.Lost or LeadStatus.Junk)
            {
                return ApiResponse<ConvertLeadResultDto>.FailureResponse($"A {lead.Status} lead cannot be converted.");
            }

            var customer = await CreateCustomerFromLeadAsync(lead, request.DisplayName, request.AssignToUserId);

            lead.Status = LeadStatus.Converted;
            lead.ConvertedCustomerId = customer.Id;
            lead.ConvertedAtUtc = DateTime.UtcNow;
            lead.UpdatedAtUtc = DateTime.UtcNow;

            await _leadRepository.UpdateAsync(lead);
            await _auditLogService.LogAsync(EntityType, id, "Converted", $"Converted to customer {customer.CustomerNumber}.", currentUserId);

            return ApiResponse<ConvertLeadResultDto>.SuccessResponse(
                new ConvertLeadResultDto { CustomerId = customer.Id, CustomerNumber = customer.CustomerNumber },
                "Lead converted to customer.");
        }

        public async Task<ApiResponse<ConvertLeadToOpportunityResultDto>> ConvertToOpportunityAsync(
            Guid id, ConvertLeadToOpportunityRequest request, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<ConvertLeadToOpportunityResultDto>.FailureResponse("Lead not found.");
            }

            if (lead.Status is LeadStatus.Lost or LeadStatus.Junk)
            {
                return ApiResponse<ConvertLeadToOpportunityResultDto>.FailureResponse($"A {lead.Status} lead cannot be converted.");
            }

            Customer customer;
            if (lead.Status == LeadStatus.Converted && lead.ConvertedCustomerId is not null)
            {
                // Already converted via the plain "Convert to Customer" action — reuse that
                // customer instead of creating a duplicate.
                var existingCustomer = await _customerRepository.GetByIdAsync(lead.ConvertedCustomerId.Value);
                if (existingCustomer is null)
                {
                    return ApiResponse<ConvertLeadToOpportunityResultDto>.FailureResponse(
                        "This lead's linked customer could not be found.");
                }
                customer = existingCustomer;
            }
            else
            {
                customer = await CreateCustomerFromLeadAsync(lead, request.CustomerDisplayName, request.AssignToUserId);

                lead.Status = LeadStatus.Converted;
                lead.ConvertedCustomerId = customer.Id;
                lead.ConvertedAtUtc = DateTime.UtcNow;
                lead.UpdatedAtUtc = DateTime.UtcNow;

                await _leadRepository.UpdateAsync(lead);
            }

            var opportunity = new Opportunity
            {
                OpportunityNumber = await _opportunityRepository.GetNextOpportunityNumberAsync(),
                Name = string.IsNullOrWhiteSpace(request.OpportunityName) ? lead.CompanyName : request.OpportunityName.Trim(),
                CustomerId = customer.Id,
                Value = request.Value ?? lead.ExpectedValue,
                ExpectedCloseDate = request.ExpectedCloseDate,
                AssignedToUserId = request.AssignToUserId ?? lead.AssignedToUserId,
                SourceLeadId = lead.Id,
                Stage = OpportunityStage.Qualification,
                Notes = lead.Notes,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _opportunityRepository.AddAsync(opportunity);
            await _auditLogService.LogAsync(EntityType, id, "Converted", $"Converted to opportunity {opportunity.OpportunityNumber}.", currentUserId);

            return ApiResponse<ConvertLeadToOpportunityResultDto>.SuccessResponse(
                new ConvertLeadToOpportunityResultDto
                {
                    CustomerId = customer.Id,
                    CustomerNumber = customer.CustomerNumber,
                    OpportunityId = opportunity.Id,
                    OpportunityNumber = opportunity.OpportunityNumber,
                },
                "Lead converted to opportunity.");
        }

        /// <summary>Builds and persists a Customer from a Lead's own fields — shared by both the plain "convert to customer" and "convert to opportunity" flows.</summary>
        private async Task<Customer> CreateCustomerFromLeadAsync(Lead lead, string? displayNameOverride, Guid? assignToUserIdOverride)
        {
            var displayName = string.IsNullOrWhiteSpace(displayNameOverride) ? lead.CompanyName : displayNameOverride.Trim();
            var assignedToUserId = assignToUserIdOverride ?? lead.AssignedToUserId;

            var customer = new Customer
            {
                CustomerNumber = await _customerRepository.GetNextCustomerNumberAsync(),
                Type = CustomerType.Business,
                LegalName = lead.CompanyName,
                DisplayName = displayName,
                Industry = lead.Industry,
                Email = lead.Email,
                Phone = lead.Mobile,
                AcquisitionSource = lead.Source,
                AssignedToUserId = assignedToUserId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                Contacts =
                [
                    new ContactPerson
                    {
                        FirstName = lead.ContactName,
                        LastName = string.Empty,
                        Email = lead.Email,
                        Mobile = lead.Mobile,
                        IsPrimary = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    },
                ],
            };

            await _customerRepository.AddAsync(customer);
            return customer;
        }

        private static LeadListItemDto MapListItem(Lead lead) => new()
        {
            Id = lead.Id,
            LeadNumber = lead.LeadNumber,
            CompanyName = lead.CompanyName,
            ContactName = lead.ContactName,
            Source = lead.Source,
            Status = lead.Status,
            ExpectedValue = lead.ExpectedValue,
            AssignedToUserId = lead.AssignedToUserId,
            AssignedToUserName = lead.AssignedToUser?.FullName,
            CreatedAtUtc = lead.CreatedAtUtc,
        };

        private static LeadDto Map(Lead lead) => new()
        {
            Id = lead.Id,
            LeadNumber = lead.LeadNumber,
            CompanyName = lead.CompanyName,
            ContactName = lead.ContactName,
            Email = lead.Email,
            Mobile = lead.Mobile,
            Industry = lead.Industry,
            Source = lead.Source,
            Campaign = lead.Campaign,
            Budget = lead.Budget,
            Timeline = lead.Timeline,
            ExpectedValue = lead.ExpectedValue,
            AssignedToUserId = lead.AssignedToUserId,
            AssignedToUserName = lead.AssignedToUser?.FullName,
            Territory = lead.Territory,
            Status = lead.Status,
            LeadScore = lead.LeadScore,
            AiScore = lead.AiScore,
            Notes = lead.Notes,
            NextFollowUpDate = lead.NextFollowUpDate,
            LostReason = lead.LostReason,
            ConvertedCustomerId = lead.ConvertedCustomerId,
            ConvertedAtUtc = lead.ConvertedAtUtc,
            CreatedAtUtc = lead.CreatedAtUtc,
            UpdatedAtUtc = lead.UpdatedAtUtc,
        };
    }
}
