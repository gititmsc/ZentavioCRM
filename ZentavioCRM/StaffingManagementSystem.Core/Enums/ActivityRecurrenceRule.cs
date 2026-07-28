namespace ZentavioCRM.Core.Enums
{
    /// <summary>How often a recurring Activity repeats. Null on the entity means the activity is a one-off.</summary>
    public enum ActivityRecurrenceRule
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }
}
