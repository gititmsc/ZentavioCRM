using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Leads
{
    /// <summary>Merges LosingLeadId into SurvivingLeadId: the losing lead's Activities/Tags are folded into the survivor, and the losing lead is deleted (Lead has no soft-delete/archive flag — see MergeResultDto.LosingRecordArchived, always false for a Lead merge).</summary>
    public class MergeLeadsRequest
    {
        [Required]
        public Guid SurvivingLeadId { get; set; }

        [Required]
        public Guid LosingLeadId { get; set; }
    }
}
