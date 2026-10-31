using CursoTracker.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CursoTracker.Api.Features.Premium;

public sealed class PremiumOptions
{
    public int FreeCoursesPerMonth { get; set; } = 3;
    public int FreeTopics { get; set; } = 2;
    /// <summary>E-mails (separados por vírgula) que podem gerar códigos premium.</summary>
    public string AdminEmails { get; set; } = "";
    /// <summary>Como falar com o dono do app para assinar (e-mail, WhatsApp...).</summary>
    public string Contact { get; set; } = "";

    public bool IsAdmin(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && AdminEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(email.Trim(), StringComparer.OrdinalIgnoreCase);
}

public static class PremiumAccess
{
    /// <summary>"Para sempre" cabe em timestamptz e em DateTimeOffset sem estourar ao somar dias.</summary>
    public static readonly DateTimeOffset Forever = new(2999, 12, 31, 0, 0, 0, TimeSpan.Zero);

    public static bool IsActive(DateTimeOffset? until, DateTimeOffset now) => until > now;

    public static async Task<bool> IsPremiumAsync(this AppDbContext db, string userId, TimeProvider time, CancellationToken ct)
    {
        var until = await db.Users.Where(u => u.Id == userId).Select(u => u.PremiumUntil).FirstOrDefaultAsync(ct);
        return IsActive(until, time.GetUtcNow());
    }

    /// <summary>402: a tela mostra o convite para o Premium.</summary>
    public static ProblemHttpResult Required(string message) =>
        TypedResults.Problem(message, statusCode: StatusCodes.Status402PaymentRequired, title: "Premium");
}
