using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Customers
{
    /// <summary>Merges LosingCustomerId into SurvivingCustomerId: every Opportunity/Quotation/Sales Order/Contact/Address/Tag/Activity and any Lead pointing at the losing customer is repointed to the survivor, and the losing customer is archived (IsActive=false) rather than deleted, since too many other records can reference a Customer to safely hard-delete it.</summary>
    public class MergeCustomersRequest
    {
        [Required]
        public Guid SurvivingCustomerId { get; set; }

        [Required]
        public Guid LosingCustomerId { get; set; }
    }
}
