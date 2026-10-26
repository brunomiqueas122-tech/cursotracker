using System.Security.Claims;

namespace CursoTracker.Api.Features.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do usuário logado; os endpoints protegidos só rodam com ele presente.</summary>
    public static string UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário sem id no cookie.");
}
