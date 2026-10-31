using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Premium;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Recommendations;

public sealed record RecommendationRequest(string? Refinement);

/// <summary>Sugestão como a tela recebe: bloqueada, ela vem sem nome, link e detalhes.</summary>
public sealed record SuggestionResponse(
    string Name,
    string Platform,
    string Url,
    string Area,
    double EstimatedHours,
    string Language,
    string Certificate,
    string Reason,
    bool Locked)
{
    public static SuggestionResponse From(CourseSuggestion s, bool locked) => locked
        ? new("", s.Platform, "", s.Area, s.EstimatedHours, s.Language, "", "", true)
        : new(s.Name, s.Platform, s.Url, s.Area, s.EstimatedHours, s.Language, s.Certificate, s.Reason, false);
}

/// <param name="FreeCoursesLeft">Cursos grátis que ainda podem ser liberados neste mês; null = Premium.</param>
public sealed record RecommendationResponse(
    IReadOnlyList<SuggestionResponse> Courses,
    string? Note,
    int RemainingSearchesToday,
    DateTimeOffset SearchedAt,
    int? FreeCoursesLeft);

public sealed class RecommendationValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationValidator() =>
        RuleFor(r => r.Refinement).MaximumLength(300).WithMessage("Use no máximo 300 caracteres no ajuste.");
}

public static class RecommendationEndpoints
{
    public static IEndpointRouteBuilder MapRecommendations(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/recommendations", Search);
        app.MapGet("/api/recommendations/latest", Latest);
        return app;
    }

    static async Task<Results<Ok<RecommendationResponse>, ValidationProblem, ProblemHttpResult>> Search(
        RecommendationRequest request,
        IValidator<RecommendationRequest> validator,
        HttpContext http,
        AppDbContext db,
        ICourseFinder finder,
        TimeProvider time,
        IOptions<AiOptions> options,
        IOptions<PremiumOptions> premium,
        ILoggerFactory loggers,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var userId = http.User.UserId();
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
            return TypedResults.Problem("Complete seu perfil antes de buscar cursos.", statusCode: StatusCodes.Status409Conflict);

        var limit = options.Value.DailySearchLimit;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var isPremium = await db.IsPremiumAsync(userId, time, ct);
        var monthly = premium.Value.FreeCoursesPerMonth;
        if (!isPremium)
        {
            // Sem cursos grátis no mês, a busca só mostraria cadeados: não gasta a busca do dia nem a cota da IA
            if (profile.FreeCoursesLeft(today, monthly) == 0)
                return PremiumAccess.Required($"Você já liberou os {monthly} cursos grátis deste mês. Com o Premium você vê todos os cursos encontrados.");
            if (profile.Topics.Count > premium.Value.FreeTopics)
                return PremiumAccess.Required($"No plano grátis a busca usa até {premium.Value.FreeTopics} assuntos. Tire alguns do seu perfil ou assine o Premium.");
        }
        if (!profile.TryUseSearch(today, limit))
            return TypedResults.Problem(
                limit == 1 ? "Você já fez a busca de hoje. A próxima fica disponível amanhã!" : $"Você já fez as {limit} buscas de hoje. Volte amanhã!",
                statusCode: StatusCodes.Status429TooManyRequests);
        // Reserva antes de chamar a IA: duas abas ao mesmo tempo não passam do limite
        await db.SaveChangesAsync(ct);

        var knownUrls = await db.Courses.Where(c => c.UserId == userId).Select(c => c.Url).ToListAsync(ct);
        try
        {
            var refinement = string.IsNullOrWhiteSpace(request.Refinement) ? null : request.Refinement.Trim();
            var result = await finder.FindAsync(profile, refinement, knownUrls, ct);
            var courses = SuggestionSanitizer.Clean(result.Courses, knownUrls);
            int? unlocked = isPremium ? null : profile.RevealCourses(today, courses.Count, monthly);
            var run = new RecommendationRun(userId, time.GetUtcNow(), refinement, result with { Courses = courses }, unlocked);
            db.RecommendationRuns.Add(run);
            await db.SaveChangesAsync(CancellationToken.None);
            return TypedResults.Ok(new RecommendationResponse(
                [.. run.Courses.Select((c, i) => SuggestionResponse.From(c, i >= (unlocked ?? int.MaxValue)))],
                run.Note, profile.RemainingSearches(today, limit), run.CreatedAt,
                isPremium ? null : profile.FreeCoursesLeft(today, monthly)));
        }
        catch (CourseSearchException e)
        {
            loggers.CreateLogger("Recommendations").LogWarning(e, "Busca com IA falhou para {UserId}", userId);
            profile.RefundSearch(today);
            await db.SaveChangesAsync(CancellationToken.None);
            return TypedResults.Problem(e.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Última busca salva, sem os cursos que a pessoa já colocou no plano.</summary>
    static async Task<Results<Ok<RecommendationResponse>, NotFound>> Latest(
        HttpContext http, AppDbContext db, TimeProvider time, IOptions<AiOptions> options, IOptions<PremiumOptions> premium, CancellationToken ct)
    {
        var userId = http.User.UserId();
        var run = await db.RecommendationRuns.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
        if (run is null)
            return TypedResults.NotFound();

        var knownUrls = await db.Courses.Where(c => c.UserId == userId).Select(c => c.Url).ToListAsync(ct);
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var remaining = profile?.RemainingSearches(today, options.Value.DailySearchLimit) ?? 0;
        // Quem virou Premium depois da busca passa a ver tudo o que ela encontrou
        var isPremium = await db.IsPremiumAsync(userId, time, ct);
        var unlockedUrls = run.Courses.Take(isPremium ? int.MaxValue : run.UnlockedCount ?? int.MaxValue).Select(c => c.Url).ToHashSet();
        var courses = SuggestionSanitizer.Clean(run.Courses, knownUrls).Select(c => SuggestionResponse.From(c, !unlockedUrls.Contains(c.Url)));
        int? freeLeft = isPremium ? null : profile?.FreeCoursesLeft(today, premium.Value.FreeCoursesPerMonth) ?? 0;
        return TypedResults.Ok(new RecommendationResponse([.. courses], run.Note, remaining, run.CreatedAt, freeLeft));
    }
}
