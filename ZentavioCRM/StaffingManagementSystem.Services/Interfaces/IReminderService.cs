namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>
    /// Turns overdue Activity due-dates and Lead follow-up dates into Notification rows for the
    /// user they're assigned to. There's no background job scheduler in this milestone, so this
    /// runs on-demand — triggered by the frontend's existing notification poll (every ~30s),
    /// piggy-backing on infrastructure that already exists rather than adding a new one.
    /// </summary>
    public interface IReminderService
    {
        /// <summary>Idempotent: only ever notifies once per due item (tracked via ReminderSentAtUtc / FollowUpReminderSentAtUtc).</summary>
        Task CheckDueRemindersAsync(Guid userId);
    }
}
