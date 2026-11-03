using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public sealed class Lead
{
    public int Id { get; set; }

    [StringLength(30)]
    public string? LeadCode { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Lead name")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(32)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    public string Company { get; set; } = string.Empty;

    [EnumDataType(typeof(LeadSource))]
    public LeadSource Source { get; set; } = LeadSource.Other;

    [EnumDataType(typeof(LeadStatus))]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    [EnumDataType(typeof(LeadPriority))]
    public LeadPriority Priority { get; set; } = LeadPriority.Normal;

    [Range(typeof(decimal), "0", "999999999999")]
    [Display(Name = "Expected value")]
    public decimal ExpectedValue { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(450)]
    public string? AssignedTo { get; set; }

    [StringLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public int? ConvertedCustomerId { get; set; }

    public Customer? ConvertedCustomer { get; set; }

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
