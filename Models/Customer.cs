using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public enum CustomerStatus
{
    Active,
    Inactive,
    Prospect
}

public sealed class Customer
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string Name => $"{FirstName} {LastName}".Trim();

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(32)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    public string Company { get; set; } = string.Empty;

    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [EnumDataType(typeof(CustomerStatus))]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [Display(Name = "Next follow-up")]
    public DateTime? NextFollowUpDate { get; set; }

    [Display(Name = "Created date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Updated date")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(450)]
    public string? AssignedTo { get; set; }

    [StringLength(450)]
    public string? CreatedBy { get; set; }

    public ICollection<CustomerActivity> Activities { get; set; } = new List<CustomerActivity>();
}
