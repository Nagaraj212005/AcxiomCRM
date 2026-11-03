using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public sealed class FollowUp : IValidatableObject
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    public Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }

    public Opportunity? Opportunity { get; set; }

    [Required, StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [EnumDataType(typeof(FollowUpType))]
    public FollowUpType Type { get; set; } = FollowUpType.Call;

    [EnumDataType(typeof(FollowUpStatus))]
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [DataType(DataType.DateTime)]
    [Display(Name = "Follow-up date")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(450)]
    public string? AssignedTo { get; set; }

    [StringLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == FollowUpStatus.Planned && FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Follow-up date cannot be earlier than today.",
                [nameof(FollowUpDate)]);
        }

        var linkCount = (CustomerId.HasValue ? 1 : 0) +
                        (LeadId.HasValue ? 1 : 0) +
                        (OpportunityId.HasValue ? 1 : 0);
        if (linkCount != 1)
        {
            yield return new ValidationResult(
                "A follow-up must be linked to exactly one customer, lead, or opportunity.",
                [nameof(CustomerId), nameof(LeadId), nameof(OpportunityId)]);
        }
    }
}
