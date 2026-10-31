using System.Security.Cryptography;
using System.Text;

namespace CursoTracker.Api.Features.Premium;

/// <summary>Código de uso único que ativa o Premium. Só o hash fica no banco.</summary>
public sealed class PremiumCode
{
    const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sem 0/O e 1/I para não confundir ao digitar
    const int Length = 12;

    public int Id { get; private set; }
    public string CodeHash { get; private set; } = "";
    /// <summary>Primeiros caracteres, para o administrador reconhecer o código na lista.</summary>
    public string Hint { get; private set; } = "";
    /// <summary>Dias de Premium; null = para sempre.</summary>
    public int? Days { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? RedeemedByEmail { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }

    private PremiumCode() { } // EF Core

    PremiumCode(string normalized, int? days, DateTimeOffset now)
    {
        CodeHash = Hash(normalized);
        Hint = normalized[..4];
        Days = days;
        CreatedAt = now;
    }

    /// <summary>Cria um código novo; o texto só existe agora, para ser mostrado uma vez.</summary>
    public static (PremiumCode Code, string Text) Generate(int? days, DateTimeOffset now)
    {
        var raw = new string(RandomNumberGenerator.GetItems<char>(Alphabet, Length));
        return (new PremiumCode(raw, days, now), $"{raw[..4]}-{raw[4..8]}-{raw[8..]}");
    }

    /// <summary>Aceita o código com ou sem traços, espaços e em minúsculas.</summary>
    public static string Normalize(string? text) =>
        new((text ?? "").ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

    public static string Hash(string normalized) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));

    /// <summary>Novo vencimento: soma os dias ao que ainda resta do Premium atual.</summary>
    public static DateTimeOffset Extend(DateTimeOffset? current, int? days, DateTimeOffset now)
    {
        if (days is null)
            return PremiumAccess.Forever;
        var start = current > now ? current.Value : now;
        return start >= PremiumAccess.Forever.AddDays(-days.Value) ? PremiumAccess.Forever : start.AddDays(days.Value);
    }
}
