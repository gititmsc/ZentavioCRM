namespace ZentavioCRM.Core.DTOs.Common
{
    /// <summary>Summary of what a Lead/Customer merge repointed, returned so the UI can show the user exactly what happened before navigating away from the (now-gone or now-archived) losing record.</summary>
    public class MergeResultDto
    {
        public Guid SurvivingId { get; set; }

        public Guid LosingId { get; set; }

        /// <summary>Number of Activity timeline entries repointed to the surviving record.</summary>
        public int ActivitiesMoved { get; set; }

        /// <summary>Number of Opportunities repointed (Customer merge only).</summary>
        public int OpportunitiesMoved { get; set; }

        /// <summary>Number of Quotations repointed (Customer merge only).</summary>
        public int QuotationsMoved { get; set; }

        /// <summary>Number of Sales Orders repointed (Customer merge only).</summary>
        public int SalesOrdersMoved { get; set; }

        /// <summary>Number of Contacts repointed (Customer merge only).</summary>
        public int ContactsMoved { get; set; }

        /// <summary>Number of Addresses repointed (Customer merge only).</summary>
        public int AddressesMoved { get; set; }

        /// <summary>Number of distinct tags merged in from the losing record.</summary>
        public int TagsMerged { get; set; }

        /// <summary>True if the losing record was archived (Customer, via IsActive=false) rather than hard-deleted (Lead).</summary>
        public bool LosingRecordArchived { get; set; }
    }
}
