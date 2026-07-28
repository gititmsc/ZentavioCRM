namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>
    /// Turns overdue Activity due-dates, Lead follow-up dates, and ContactPerson birthdays/anniversaries
    /// into Notification rows for the user they're assigned to. There's no background job scheduler in
    /// this milestone, so this runs on-demand — triggered by the frontend's existing notification poll
    /// (every ~30s), piggy-backing on infrastructure that already exists rather than adding a new one.
    /// </summary>
    public interface IReminderService
    {
        /// <summary>
        /// Idempotent: only ever notifies once per due item, tracked via ReminderSentAtUtc / FollowUpReminderSentAtUtc
        /// for Activities/Leads, and BirthdayReminderSentYear / AnniversaryReminderSentYear (compared by calendar
        /// year, since birthdays/anniversaries recur annually) for Contacts.
        /// </summary>
        Task CheckDueRemindersAsync(Guid userId);

        /// <summary>
        /// For every active out-of-office delegation where the given user is the delegate, runs the
        /// same due-item checks against the delegator's assigned records, but notifies the delegate
        /// instead — and still stamps the delegator's own reminder-sent markers, so the delegator
        /// doesn't get a duplicate notification once they return and their own poll runs.
        /// </summary>
        Task CheckDelegatedRemindersAsync(Guid delegateUserId);
    }
}
