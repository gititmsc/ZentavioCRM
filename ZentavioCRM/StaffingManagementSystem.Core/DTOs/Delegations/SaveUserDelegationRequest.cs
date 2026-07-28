using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Delegations
{
    /// <summary>Always creates a delegation FROM the current (authenticated) user — self-service out-of-office setup, not something one user sets up on another's behalf.</summary>
    public class SaveUserDelegationRequest
    {
        [Required(ErrorMessage = "A delegate must be selected.")]
        public Guid DelegateUserId { get; set; }

        [Required(ErrorMessage = "A start date is required.")]
        public DateTime StartDateUtc { get; set; }

        [Required(ErrorMessage = "An end date is required.")]
        public DateTime EndDateUtc { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
