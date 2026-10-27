using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Recommendations;

public sealed record RecommendationRequest(string? Refinement);

public sealed record RecommendationResponse(
    IReadOnlyList<CourseSuggestion> Courses,
    string? Note,
    int RemainingSearchesToday,
    DateTimeOffset SearchedAt);

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
            var run = new RecommendationRun(userId, time.GetUtcNow(), refinement,
                result with { Courses = SuggestionSanitizer.Clean(result.Courses, knownUrls) });
            db.RecommendationRuns.Add(run);
            await db.SaveChangesAsync(CancellationToken.None);
            return TypedResults.Ok(new RecommendationResponse(run.Courses, run.Note, profile.RemainingSearches(today, limit), run.CreatedAt));
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
        HttpContext http, AppDbContext db, TimeProvider time, IOptions<AiOptions> options, CancellationToken ct)
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
        var remaining = profile?.RemainingSearches(DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime), options.Value.DailySearchLimit) ?? 0;
        return TypedResults.Ok(new RecommendationResponse(SuggestionSanitizer.Clean(run.Courses, knownUrls), run.Note, remaining, run.CreatedAt));
    }
}
