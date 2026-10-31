namespace CursoTracker.Api.Features.Recommendations;

/// <summary>Resultado de uma busca com IA, guardado para ser revisto sem gastar outra busca.</summary>
public sealed class RecommendationRun
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Refinement { get; private set; }
    public string? Note { get; private set; }
    public List<CourseSuggestion> Courses { get; private set; } = [];
    /// <summary>Quantos dos primeiros cursos ficaram liberados no plano grátis; null = todos (Premium).</summary>
    public int? UnlockedCount { get; private set; }

    private RecommendationRun() { } // EF Core

    public RecommendationRun(string userId, DateTimeOffset createdAt, string? refinement, CourseSearchResult result, int? unlockedCount)
    {
        UserId = userId;
        CreatedAt = createdAt;
        Refinement = refinement;
        Note = result.Note;
        Courses = [.. result.Courses];
        UnlockedCount = unlockedCount;
    }
}
