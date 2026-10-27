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

    private RecommendationRun() { } // EF Core

    public RecommendationRun(string userId, DateTimeOffset createdAt, string? refinement, CourseSearchResult result)
    {
        UserId = userId;
        CreatedAt = createdAt;
        Refinement = refinement;
        Note = result.Note;
        Courses = [.. result.Courses];
    }
}
