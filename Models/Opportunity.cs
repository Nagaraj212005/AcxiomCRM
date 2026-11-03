using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public sealed class Opportunity : IValidatableObject
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Opportunity name")]
    public string Name { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    public Lead? Lead { get; set; }

    [Required]
    [EnumDataType(typeof(OpportunityStage))]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal Amount { get; set; }

    [Range(0, 100)]
    public int Probability { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Expected close date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);

    [StringLength(100)]
    public string Source { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(450)]
    public string? AssignedTo { get; set; }

    [StringLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public decimal WeightedPipeline => Amount * Probability / 100m;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Stage is not OpportunityStage.Won and not OpportunityStage.Lost &&
            ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Expected Close Date cannot be in the past.",
                [nameof(ExpectedCloseDate)]);
        }

        if (CustomerId.HasValue == LeadId.HasValue)
        {
            yield return new ValidationResult(
                "Select exactly one customer or lead for this opportunity.",
                [nameof(CustomerId), nameof(LeadId)]);
        }
    }
}
