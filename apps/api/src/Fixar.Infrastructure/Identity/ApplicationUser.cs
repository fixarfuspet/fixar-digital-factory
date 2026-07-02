using Microsoft.AspNetCore.Identity;

namespace Fixar.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user, extended with the fields FIXAR OS needs.
/// Deliberately lives in Infrastructure (not Domain) so the Domain layer
/// stays free of any dependency on Microsoft.AspNetCore.Identity.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
