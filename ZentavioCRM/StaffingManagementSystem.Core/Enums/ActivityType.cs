namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Kind of interaction recorded by an <see cref="Entities.Activity"/> timeline entry.
    /// </summary>
    public enum ActivityType
    {
        Call = 1,
        Email = 2,
        Meeting = 3,
        Task = 4,
        Note = 5,
        Visit = 6,
        WhatsApp = 7,
        Sms = 8
    }
}
