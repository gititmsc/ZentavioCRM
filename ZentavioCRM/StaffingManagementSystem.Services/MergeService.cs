using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IMergeService"/>
    public class MergeService : IMergeService
    {
        private readonly IMergeRepository _mergeRepository;
        private readonly ILeadRepository _leadRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAuditLogService _auditLogService;

        public MergeService(
            IMergeRepository mergeRepository,
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            IAuditLogService auditLogService)
        {
            _mergeRepository = mergeRepository;
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _auditLogService = auditLogService;
        }

        public async Task<ApiResponse<MergeResultDto>> MergeLeadsAsync(Guid survivingLeadId, Guid losingLeadId, Guid? currentUserId)
        {
            if (survivingLeadId == losingLeadId)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("Cannot merge a lead into itself.");
            }

            var surviving = await _leadRepository.GetByIdAsync(survivingLeadId);
            if (surviving is null)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("The lead you want to keep could not be found.");
            }

            var losing = await _leadRepository.GetByIdAsync(losingLeadId);
            if (losing is null)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("The duplicate lead could not be found.");
            }

            MergeResultDto result;
            try
            {
                result = await _mergeRepository.MergeLeadsAsync(survivingLeadId, losingLeadId);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<MergeResultDto>.FailureResponse(ex.Message);
            }

            await _auditLogService.LogAsync(
                "Lead", survivingLeadId, "Merged",
                $"Merged lead {losing.LeadNumber} ({losing.CompanyName}) into this lead — {result.ActivitiesMoved} activit{(result.ActivitiesMoved == 1 ? "y" : "ies")}, {result.TagsMerged} tag(s) carried over.",
                currentUserId);
            await _auditLogService.LogAsync(
                "Lead", losingLeadId, "MergedAway",
                $"Merged into lead {surviving.LeadNumber} ({surviving.CompanyName}) and deleted.",
                currentUserId);

            return ApiResponse<MergeResultDto>.SuccessResponse(result, $"Merged {losing.LeadNumber} into {surviving.LeadNumber}.");
        }

        public async Task<ApiResponse<MergeResultDto>> MergeCustomersAsync(Guid survivingCustomerId, Guid losingCustomerId, Guid? currentUserId)
        {
            if (survivingCustomerId == losingCustomerId)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("Cannot merge a customer into itself.");
            }

            var surviving = await _customerRepository.GetByIdAsync(survivingCustomerId);
            if (surviving is null)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("The customer you want to keep could not be found.");
            }

            var losing = await _customerRepository.GetByIdAsync(losingCustomerId);
            if (losing is null)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("The duplicate customer could not be found.");
            }

            if (!losing.IsActive)
            {
                return ApiResponse<MergeResultDto>.FailureResponse("That customer has already been archived (merged away previously).");
            }

            MergeResultDto result;
            try
            {
                result = await _mergeRepository.MergeCustomersAsync(survivingCustomerId, losingCustomerId);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<MergeResultDto>.FailureResponse(ex.Message);
            }

            await _auditLogService.LogAsync(
                "Customer", survivingCustomerId, "Merged",
                $"Merged customer {losing.CustomerNumber} ({losing.DisplayName}) into this customer — " +
                $"{result.OpportunitiesMoved} opportunit{(result.OpportunitiesMoved == 1 ? "y" : "ies")}, {result.QuotationsMoved} quotation(s), " +
                $"{result.SalesOrdersMoved} sales order(s), {result.ContactsMoved} contact(s), {result.AddressesMoved} address(es), " +
                $"{result.ActivitiesMoved} activit{(result.ActivitiesMoved == 1 ? "y" : "ies")}, {result.TagsMerged} tag(s) carried over.",
                currentUserId);
            await _auditLogService.LogAsync(
                "Customer", losingCustomerId, "MergedAway",
                $"Merged into customer {surviving.CustomerNumber} ({surviving.DisplayName}) and archived.",
                currentUserId);

            return ApiResponse<MergeResultDto>.SuccessResponse(result, $"Merged {losing.CustomerNumber} into {surviving.CustomerNumber}.");
        }
    }
}
