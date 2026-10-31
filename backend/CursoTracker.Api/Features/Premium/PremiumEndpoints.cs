using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Premium;

public sealed record PremiumStatusResponse(bool IsPremium, DateTimeOffset? PremiumUntil, bool Forever, string? Contact);
public sealed record RedeemRequest(string Code);
public sealed record CreateCodeRequest(int? Days);
public sealed record CreatedCodeResponse(string Code, int? Days);
public sealed record PremiumCodeResponse(int Id, string Hint, int? Days, DateTimeOffset CreatedAt, string? RedeemedByEmail, DateTimeOffset? RedeemedAt);

public sealed class CreateCodeValidator : AbstractValidator<CreateCodeRequest>
{
    public CreateCodeValidator() =>
        RuleFor(r => r.Days).InclusiveBetween(1, 3650).When(r => r.Days is not null).WithMessage("Use entre 1 e 3650 dias.");
}

public static class PremiumEndpoints
{
    public static IEndpointRouteBuilder MapPremium(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/premium");
        group.MapGet("/", Status);
        // Mesmo limite do login: impede tentar códigos em massa
        group.MapPost("/redeem", Redeem).RequireRateLimiting(AuthEndpoints.RateLimitPolicy);
        group.MapGet("/codes", ListCodes);
        group.MapPost("/codes", CreateCode);
        return app;
    }

    static async Task<Ok<PremiumStatusResponse>> Status(
        HttpContext http, AppDbContext db, TimeProvider time, IOptions<PremiumOptions> options, CancellationToken ct)
    {
        var until = await db.Users.Where(u => u.Id == http.User.UserId()).Select(u => u.PremiumUntil).FirstOrDefaultAsync(ct);
        return TypedResults.Ok(ToStatus(until, time.GetUtcNow(), options.Value));
    }

    static async Task<Results<Ok<PremiumStatusResponse>, ValidationProblem>> Redeem(
        RedeemRequest request, HttpContext http, AppDbContext db, TimeProvider time, IOptions<PremiumOptions> options, CancellationToken ct)
    {
        var invalid = TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["Code"] = ["Código inválido ou já usado."] });
        var normalized = PremiumCode.Normalize(request.Code);
        if (normalized.Length == 0)
            return invalid;

        var hash = PremiumCode.Hash(normalized);
        var code = await db.PremiumCodes.AsNoTracking().FirstOrDefaultAsync(c => c.CodeHash == hash && c.RedeemedAt == null, ct);
        if (code is null)
            return invalid;

        var now = time.GetUtcNow();
        var user = await db.Users.FirstAsync(u => u.Id == http.User.UserId(), ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Marca como usado só se ninguém usou antes: dois cliques ao mesmo tempo não ativam duas vezes
        var claimed = await db.PremiumCodes
            .Where(c => c.Id == code.Id && c.RedeemedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RedeemedAt, now).SetProperty(c => c.RedeemedByEmail, user.Email), ct);
        if (claimed == 0)
            return invalid;

        user.PremiumUntil = PremiumCode.Extend(user.PremiumUntil, code.Days, now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return TypedResults.Ok(ToStatus(user.PremiumUntil, now, options.Value));
    }

    static async Task<Results<Ok<List<PremiumCodeResponse>>, ForbidHttpResult>> ListCodes(
        HttpContext http, AppDbContext db, IOptions<PremiumOptions> options, CancellationToken ct)
    {
        if (!options.Value.IsAdmin(http.User.Identity?.Name))
            return TypedResults.Forbid();

        var codes = await db.PremiumCodes.AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Take(50)
            .Select(c => new PremiumCodeResponse(c.Id, c.Hint, c.Days, c.CreatedAt, c.RedeemedByEmail, c.RedeemedAt))
            .ToListAsync(ct);
        return TypedResults.Ok(codes);
    }

    static async Task<Results<Ok<CreatedCodeResponse>, ValidationProblem, ForbidHttpResult>> CreateCode(
        CreateCodeRequest request,
        IValidator<CreateCodeRequest> validator,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        IOptions<PremiumOptions> options,
        CancellationToken ct)
    {
        if (!options.Value.IsAdmin(http.User.Identity?.Name))
            return TypedResults.Forbid();
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var (code, text) = PremiumCode.Generate(request.Days, time.GetUtcNow());
        db.PremiumCodes.Add(code);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new CreatedCodeResponse(text, code.Days));
    }

    static PremiumStatusResponse ToStatus(DateTimeOffset? until, DateTimeOffset now, PremiumOptions options)
    {
        var active = PremiumAccess.IsActive(until, now);
        return new(active, active ? until : null, active && until >= PremiumAccess.Forever,
            string.IsNullOrWhiteSpace(options.Contact) ? null : options.Contact.Trim());
    }
}
