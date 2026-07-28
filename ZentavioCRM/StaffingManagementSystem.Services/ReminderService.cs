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
        private readonly IUserDelegationRepository _userDelegationRepository;

        public ReminderService(
            IActivityRepository activityRepository,
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            INotificationService notificationService,
            IUserDelegationRepository userDelegationRepository)
        {
            _activityRepository = activityRepository;
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _notificationService = notificationService;
            _userDelegationRepository = userDelegationRepository;
        }

        public Task CheckDueRemindersAsync(Guid userId) => CheckAndNotifyAsync(ownerUserId: userId, notifyUserId: userId);

        public async Task CheckDelegatedRemindersAsync(Guid delegateUserId)
        {
            var activeDelegations = await _userDelegationRepository.GetActiveForDelegateAsync(delegateUserId, DateTime.UtcNow);

            foreach (var delegation in activeDelegations)
            {
                // Reuses the exact same due-item queries and reminder-sent stamping as the delegator's
                // own check (so the delegator doesn't ALSO get double-notified once they return and their
                // own poll runs), just redirecting who the notification goes to.
                await CheckAndNotifyAsync(ownerUserId: delegation.DelegatorUserId, notifyUserId: delegateUserId);
            }
        }

        private async Task CheckAndNotifyAsync(Guid ownerUserId, Guid notifyUserId)
        {
            var now = DateTime.UtcNow;

            var dueActivities = await _activityRepository.GetDueForReminderAsync(ownerUserId, now);
            foreach (var activity in dueActivities)
            {
                await _notificationService.NotifyAsync(
                    notifyUserId,
                    $"\"{activity.Subject}\" ({activity.Type}) was due {activity.DueAtUtc:MMM d} and is still open.",
                    activity.RelatedToType,
                    activity.RelatedToId);

                activity.ReminderSentAtUtc = now;
                await _activityRepository.UpdateAsync(activity);
            }

            var dueLeads = await _leadRepository.GetDueForFollowUpReminderAsync(ownerUserId, now);
            foreach (var lead in dueLeads)
            {
                await _notificationService.NotifyAsync(
                    notifyUserId,
                    $"Follow-up due for lead {lead.LeadNumber} — {lead.CompanyName}.",
                    RelatedEntityType.Lead,
                    lead.Id);

                lead.FollowUpReminderSentAtUtc = now;
                await _leadRepository.UpdateAsync(lead);
            }

            var dueBirthdays = await _customerRepository.GetDueForBirthdayReminderAsync(ownerUserId, now);
            foreach (var contact in dueBirthdays)
            {
                await _notificationService.NotifyAsync(
                    notifyUserId,
                    $"Today is {contact.FullName}'s birthday — a good time to reach out ({contact.Customer!.DisplayName}).",
                    RelatedEntityType.Customer,
                    contact.CustomerId);

                contact.BirthdayReminderSentYear = now.Year;
                await _customerRepository.UpdateContactAsync(contact);
            }

            var dueAnniversaries = await _customerRepository.GetDueForAnniversaryReminderAsync(ownerUserId, now);
            foreach (var contact in dueAnniversaries)
            {
                await _notificationService.NotifyAsync(
                    notifyUserId,
                    $"Today is {contact.FullName}'s anniversary — a good time to reach out ({contact.Customer!.DisplayName}).",
                    RelatedEntityType.Customer,
                    contact.CustomerId);

                contact.AnniversaryReminderSentYear = now.Year;
                await _customerRepository.UpdateContactAsync(contact);
            }
        }
    }
}
