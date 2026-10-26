using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Recommendations;

public sealed record RecommendationRequest(string? Refinement);

public sealed record RecommendationResponse(IReadOnlyList<CourseSuggestion> Courses, string? Note, int RemainingSearchesToday);

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
            return TypedResults.Problem($"Você já fez as {limit} buscas de hoje. Volte amanhã!", statusCode: StatusCodes.Status429TooManyRequests);
        // Reserva antes de chamar a IA: duas abas ao mesmo tempo não passam do limite
        await db.SaveChangesAsync(ct);

        var knownUrls = await db.Courses.Where(c => c.UserId == userId).Select(c => c.Url).ToListAsync(ct);
        try
        {
            var result = await finder.FindAsync(profile, request.Refinement?.Trim(), knownUrls, ct);
            var courses = SuggestionSanitizer.Clean(result.Courses, knownUrls);
            return TypedResults.Ok(new RecommendationResponse(courses, result.Note, profile.RemainingSearches(today, limit)));
        }
        catch (CourseSearchException e)
        {
            loggers.CreateLogger("Recommendations").LogWarning(e, "Busca com IA falhou para {UserId}", userId);
            profile.RefundSearch(today);
            await db.SaveChangesAsync(CancellationToken.None);
            return TypedResults.Problem(e.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
