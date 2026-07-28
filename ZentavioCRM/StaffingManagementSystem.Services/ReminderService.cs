using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IReminderService"/>
    public class ReminderService : IReminderService
    {
        private readonly IActivityRepository _activityRepository;
        private readonly ILeadRepository _leadRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly INotificationService _notificationService;

        public ReminderService(
            IActivityRepository activityRepository,
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            INotificationService notificationService)
        {
            _activityRepository = activityRepository;
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _notificationService = notificationService;
        }

        public async Task CheckDueRemindersAsync(Guid userId)
        {
            var now = DateTime.UtcNow;

            var dueActivities = await _activityRepository.GetDueForReminderAsync(userId, now);
            foreach (var activity in dueActivities)
            {
                await _notificationService.NotifyAsync(
                    userId,
                    $"\"{activity.Subject}\" ({activity.Type}) was due {activity.DueAtUtc:MMM d} and is still open.",
                    activity.RelatedToType,
                    activity.RelatedToId);

                activity.ReminderSentAtUtc = now;
                await _activityRepository.UpdateAsync(activity);
            }

            var dueLeads = await _leadRepository.GetDueForFollowUpReminderAsync(userId, now);
            foreach (var lead in dueLeads)
            {
                await _notificationService.NotifyAsync(
                    userId,
                    $"Follow-up due for lead {lead.LeadNumber} — {lead.CompanyName}.",
                    RelatedEntityType.Lead,
                    lead.Id);

                lead.FollowUpReminderSentAtUtc = now;
                await _leadRepository.UpdateAsync(lead);
            }

            var dueBirthdays = await _customerRepository.GetDueForBirthdayReminderAsync(userId, now);
            foreach (var contact in dueBirthdays)
            {
                await _notificationService.NotifyAsync(
                    userId,
                    $"Today is {contact.FullName}'s birthday — a good time to reach out ({contact.Customer!.DisplayName}).",
                    RelatedEntityType.Customer,
                    contact.CustomerId);

                contact.BirthdayReminderSentYear = now.Year;
                await _customerRepository.UpdateContactAsync(contact);
            }

            var dueAnniversaries = await _customerRepository.GetDueForAnniversaryReminderAsync(userId, now);
            foreach (var contact in dueAnniversaries)
            {
                await _notificationService.NotifyAsync(
                    userId,
                    $"Today is {contact.FullName}'s anniversary — a good time to reach out ({contact.Customer!.DisplayName}).",
                    RelatedEntityType.Customer,
                    contact.CustomerId);

                contact.AnniversaryReminderSentYear = now.Year;
                await _customerRepository.UpdateContactAsync(contact);
            }
        }
    }
}
