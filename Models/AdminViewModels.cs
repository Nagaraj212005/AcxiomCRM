namespace AcxiomCRM.Models;

public sealed class UserAdministrationViewModel
{
    public IReadOnlyList<UserAdministrationItemViewModel> Users { get; init; } = [];

    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class UserAdministrationItemViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public DateTimeOffset? LockoutEnd { get; init; }

    public bool IsPermanentlyDisabled { get; init; }
}
