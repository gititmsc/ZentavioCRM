namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// How much of the record-set a <see cref="Entities.Role"/> can see/act on for the ownable
    /// modules (Leads, Customers, Opportunities). Applied uniformly across those modules rather
    /// than configured per-module, to keep the model easy to reason about.
    /// </summary>
    public enum VisibilityScope
    {
        /// <summary>Only records the user is assigned to (or created, for not-yet-assigned records).</summary>
        Own = 1,

        /// <summary>Own records, plus records assigned to (or created by) anyone in the same Department.</summary>
        Team = 2,

        /// <summary>Every record — the historical default behavior, preserved so existing roles are unaffected until an admin explicitly narrows them.</summary>
        All = 3
    }
}
