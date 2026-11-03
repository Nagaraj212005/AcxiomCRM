using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public sealed class CrmActivity : IValidatableObject
{
    public int Id { get; set; }

    [EnumDataType(typeof(ActivityType))]
    public ActivityType Type { get; set; }

    [Required, StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [EnumDataType(typeof(ActivityStatus))]
    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    public Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }

    public Opportunity? Opportunity { get; set; }

    [StringLength(450)]
    public string? AssignedTo { get; set; }

    [StringLength(450)]
    public string? CreatedBy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var linkCount = (CustomerId.HasValue ? 1 : 0) +
                        (LeadId.HasValue ? 1 : 0) +
                        (OpportunityId.HasValue ? 1 : 0);
        if (linkCount != 1)
        {
            yield return new ValidationResult(
                "An activity must be linked to exactly one customer, lead, or opportunity.",
                [nameof(CustomerId), nameof(LeadId), nameof(OpportunityId)]);
        }
    }
}
