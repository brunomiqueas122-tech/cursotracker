using System.Security.Cryptography;
using System.Text;
using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Premium;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Auth;

public sealed record RegisterRequest(string Email, string Password, string InviteCode);
public sealed record LoginRequest(string Email, string Password);
public sealed record MeResponse(string Email, bool HasProfile, bool IsPremium, bool IsAdmin);

public static class AuthEndpoints
{
    public const string RateLimitPolicy = "auth";

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/register", Register).RequireRateLimiting(RateLimitPolicy);
        group.MapPost("/login", Login).RequireRateLimiting(RateLimitPolicy);
        group.MapPost("/logout", (Delegate)Logout); // cast: senão vira RequestDelegate e o 204 tipado é ignorado
        group.MapGet("/me", Me).RequireAuthorization();
        return app;
    }

    static async Task<Results<NoContent, ValidationProblem>> Register(
        RegisterRequest request, UserManager<AppUser> users, SignInManager<AppUser> signIn, IConfiguration config)
    {
        // Convite obrigatório: cada busca com IA custa dinheiro na chave do dono do app
        if (!Matches(request.InviteCode ?? "", config["Auth:InviteCode"] ?? ""))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["InviteCode"] = ["Código de convite inválido."] });

        var email = request.Email?.Trim() ?? "";
        var user = new AppUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, request.Password ?? "");
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "Password" : "Email")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).Distinct().ToArray());
            return TypedResults.ValidationProblem(errors);
        }

        await signIn.SignInAsync(user, isPersistent: true);
        return TypedResults.NoContent();
    }

    static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> Login(
        LoginRequest request, SignInManager<AppUser> signIn)
    {
        var result = await signIn.PasswordSignInAsync(request.Email?.Trim() ?? "", request.Password ?? "", isPersistent: true, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return TypedResults.Problem("Muitas tentativas erradas. Tente de novo em alguns minutos.", statusCode: StatusCodes.Status429TooManyRequests);
        return result.Succeeded ? TypedResults.NoContent() : TypedResults.Unauthorized();
    }

    static async Task<NoContent> Logout(SignInManager<AppUser> signIn)
    {
        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    static async Task<Ok<MeResponse>> Me(
        HttpContext http, AppDbContext db, TimeProvider time, IOptions<PremiumOptions> premium, CancellationToken ct)
    {
        var userId = http.User.UserId();
        var hasProfile = await db.Profiles.AnyAsync(p => p.UserId == userId, ct);
        var email = http.User.Identity?.Name ?? "";
        return TypedResults.Ok(new MeResponse(email, hasProfile, await db.IsPremiumAsync(userId, time, ct), premium.Value.IsAdmin(email)));
    }

    // Compara os hashes em tempo constante para não vazar o código por tempo de resposta
    static bool Matches(string given, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(given)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
