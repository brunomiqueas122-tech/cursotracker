using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Cursos;

namespace CursoTracker.Api.Features.Recommendations;

public sealed class AiOptions
{
    /// <summary>"Gemini" (cota grátis do Google) ou "Claude" (Anthropic, pago).</summary>
    public string Provider { get; set; } = "Gemini";
    public string GeminiModel { get; set; } = "gemini-2.5-flash";
    public string ClaudeModel { get; set; } = "claude-opus-5-5";
    public int DailySearchLimit { get; set; } = 1;
    public int MaxWebSearches { get; set; } = 6;
}

public sealed record CourseSuggestion(
    string Name,
    string Platform,
    string Url,
    string Area,
    double EstimatedHours,
    string Language,
    string Certificate,
    string Reason);

public sealed record CourseSearchResult(IReadOnlyList<CourseSuggestion> Courses, string? Note);

/// <summary>Busca cursos gratuitos com certificado para um perfil (Gemini ou Claude, ambos com busca na web).</summary>
public interface ICourseFinder
{
    Task<CourseSearchResult> FindAsync(UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls, CancellationToken ct);
}

/// <summary>A IA falhou ou recusou; a mensagem já está pronta para o usuário.</summary>
public sealed class CourseSearchException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Resultados vêm da web via IA: só passa o que é seguro e útil mostrar.</summary>
public static class SuggestionSanitizer
{
    public const int MaxResults = 10;

    public static IReadOnlyList<CourseSuggestion> Clean(IEnumerable<CourseSuggestion> suggestions, IReadOnlyCollection<string> knownUrls)
    {
        var seen = new HashSet<string>(knownUrls.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        var result = new List<CourseSuggestion>();
        foreach (var s in suggestions)
        {
            var url = s.Url?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s.Name) || !Urls.IsHttp(url) || !seen.Add(Normalize(url)))
                continue;

            result.Add(new CourseSuggestion(
                Truncate(s.Name, 200),
                Truncate(s.Platform, 100),
                Truncate(url, 500),
                Truncate(string.IsNullOrWhiteSpace(s.Area) ? "Geral" : s.Area, 100),
                Math.Clamp(Math.Round(s.EstimatedHours * 2) / 2, 0.5, 500),
                Truncate(s.Language, 40),
                Truncate(s.Certificate, 300),
                Truncate(s.Reason, 500)));
            if (result.Count == MaxResults)
                break;
        }
        return result;
    }

    static string Normalize(string url) => url.Trim().TrimEnd('/');

    static string Truncate(string? value, int max)
    {
        var v = value?.Trim() ?? "";
        return v.Length <= max ? v : v[..max];
    }
}
