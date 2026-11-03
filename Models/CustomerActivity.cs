using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public sealed class CustomerActivity
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    [Required, StringLength(40)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(201)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(450)]
    public string? UserId { get; set; }

    [Required, StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
