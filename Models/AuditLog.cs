using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public sealed class AuditLog
{
    public long Id { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [Required, StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Module { get; set; } = string.Empty;

    [StringLength(80)]
    public string? EntityName { get; set; }

    [StringLength(80)]
    public string? RecordId { get; set; }

    [StringLength(20)]
    public string Result { get; set; } = "Success";

    [StringLength(2000)]
    public string? OldValue { get; set; }

    [StringLength(2000)]
    public string? NewValue { get; set; }

    [StringLength(2000)]
    public string? Details { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
