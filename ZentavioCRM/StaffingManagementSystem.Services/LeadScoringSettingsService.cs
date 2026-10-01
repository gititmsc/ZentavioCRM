using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ILeadScoringSettingsService"/>
    public class LeadScoringSettingsService : ILeadScoringSettingsService
    {
        private const string EntityType = "LeadScoringSettings";

        private readonly ILeadScoringSettingsRepository _repository;
        private readonly IAuditLogService _auditLogService;

        public LeadScoringSettingsService(ILeadScoringSettingsRepository repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<LeadScoringSettingsDto> GetAsync()
        {
            var settings = await _repository.GetOrCreateAsync();
            return Map(settings);
        }

        public async Task<ApiResponse<LeadScoringSettingsDto>> UpdateAsync(LeadScoringSettingsDto request, Guid? currentUserId)
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return ApiResponse<LeadScoringSettingsDto>.FailureResponse("Please correct the highlighted field(s) and try again.", errors);
            }

            var settings = await _repository.GetOrCreateAsync();

            settings.EmailPresentPoints = request.EmailPresentPoints;
            settings.MobilePresentPoints = request.MobilePresentPoints;
            settings.IndustryPresentPoints = request.IndustryPresentPoints;
            settings.AssignedPoints = request.AssignedPoints;
            settings.ExpectedValueHighThreshold = request.ExpectedValueHighThreshold;
            settings.ExpectedValueHighPoints = request.ExpectedValueHighPoints;
            settings.ExpectedValueMediumThreshold = request.ExpectedValueMediumThreshold;
            settings.ExpectedValueMediumPoints = request.ExpectedValueMediumPoints;
            settings.ExpectedValueLowPoints = request.ExpectedValueLowPoints;
            settings.SourceReferralPoints = request.SourceReferralPoints;
            settings.SourceWarmChannelPoints = request.SourceWarmChannelPoints;
            settings.UrgentTimelinePoints = request.UrgentTimelinePoints;
            settings.PointsPerCompletedActivity = request.PointsPerCompletedActivity;
            settings.EngagementMaxPoints = request.EngagementMaxPoints;
            settings.MaxScore = request.MaxScore;
            settings.UpdatedByUserId = currentUserId;
            settings.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.UpdateAsync(settings);
            await _auditLogService.LogAsync(EntityType, settings.Id, "Updated", "Lead scoring settings updated.", currentUserId);

            return ApiResponse<LeadScoringSettingsDto>.SuccessResponse(Map(settings), "Lead scoring settings saved.");
        }

        /// <summary>Every weight must be non-negative (a negative point value would make the formula subtract instead of add, which isn't what this screen is for), and the two ExpectedValue thresholds must be in ascending order so the medium/high tiers in LeadService.ComputeLeadScore stay meaningful.</summary>
        private static List<string> Validate(LeadScoringSettingsDto request)
        {
            var errors = new List<string>();

            void RequireNonNegative(int value, string fieldName)
            {
                if (value < 0) errors.Add($"{fieldName} cannot be negative.");
            }

            RequireNonNegative(request.EmailPresentPoints, "Email present points");
            RequireNonNegative(request.MobilePresentPoints, "Mobile present points");
            RequireNonNegative(request.IndustryPresentPoints, "Industry present points");
            RequireNonNegative(request.AssignedPoints, "Assigned points");
            RequireNonNegative(request.ExpectedValueHighPoints, "Expected value (high) points");
            RequireNonNegative(request.ExpectedValueMediumPoints, "Expected value (medium) points");
            RequireNonNegative(request.ExpectedValueLowPoints, "Expected value (low) points");
            RequireNonNegative(request.SourceReferralPoints, "Referral source points");
            RequireNonNegative(request.SourceWarmChannelPoints, "Warm-channel source points");
            RequireNonNegative(request.UrgentTimelinePoints, "Urgent timeline points");
            RequireNonNegative(request.PointsPerCompletedActivity, "Points per completed activity");
            RequireNonNegative(request.EngagementMaxPoints, "Engagement max points");

            if (request.ExpectedValueHighThreshold < 0) errors.Add("Expected value (high) threshold cannot be negative.");
            if (request.ExpectedValueMediumThreshold < 0) errors.Add("Expected value (medium) threshold cannot be negative.");
            if (request.ExpectedValueMediumThreshold > request.ExpectedValueHighThreshold)
            {
                errors.Add("Expected value (medium) threshold must be less than or equal to the high threshold.");
            }

            if (request.MaxScore < 1) errors.Add("Max score must be at least 1.");

            return errors;
        }

        private static LeadScoringSettingsDto Map(LeadScoringSettings settings) => new()
        {
            EmailPresentPoints = settings.EmailPresentPoints,
            MobilePresentPoints = settings.MobilePresentPoints,
            IndustryPresentPoints = settings.IndustryPresentPoints,
            AssignedPoints = settings.AssignedPoints,
            ExpectedValueHighThreshold = settings.ExpectedValueHighThreshold,
            ExpectedValueHighPoints = settings.ExpectedValueHighPoints,
            ExpectedValueMediumThreshold = settings.ExpectedValueMediumThreshold,
            ExpectedValueMediumPoints = settings.ExpectedValueMediumPoints,
            ExpectedValueLowPoints = settings.ExpectedValueLowPoints,
            SourceReferralPoints = settings.SourceReferralPoints,
            SourceWarmChannelPoints = settings.SourceWarmChannelPoints,
            UrgentTimelinePoints = settings.UrgentTimelinePoints,
            PointsPerCompletedActivity = settings.PointsPerCompletedActivity,
            EngagementMaxPoints = settings.EngagementMaxPoints,
            MaxScore = settings.MaxScore,
            UpdatedAtUtc = settings.UpdatedAtUtc,
        };
    }
}
