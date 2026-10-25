using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CursoTracker.Api.Features.Auth;

public sealed record LoginRequest(string Password);

public static class AuthEndpoints
{
    public const string LoginRateLimit = "login";

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/login", Login).RequireRateLimiting(LoginRateLimit);
        group.MapPost("/logout", (Delegate)Logout); // cast: senão vira RequestDelegate e o 204 tipado é ignorado
        group.MapGet("/me", () => TypedResults.NoContent()).RequireAuthorization();
        return app;
    }

    static async Task<Results<NoContent, UnauthorizedHttpResult>> Login(
        LoginRequest request, HttpContext http, IConfiguration config)
    {
        var expected = config["Auth:Password"];
        if (string.IsNullOrEmpty(expected) || !PasswordMatches(request.Password ?? "", expected))
            return TypedResults.Unauthorized();

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "owner")], CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
        return TypedResults.NoContent();
    }

    static async Task<NoContent> Logout(HttpContext http)
    {
        await http.SignOutAsync();
        return TypedResults.NoContent();
    }

    // Compara os hashes em tempo constante para não vazar a senha por tempo de resposta
    static bool PasswordMatches(string given, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(given)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
