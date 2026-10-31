using Microsoft.AspNetCore.Identity;

namespace CursoTracker.Api.Features.Auth;

public sealed class AppUser : IdentityUser
{
    /// <summary>Até quando o Premium vale; null = plano grátis.</summary>
    public DateTimeOffset? PremiumUntil { get; set; }
}
