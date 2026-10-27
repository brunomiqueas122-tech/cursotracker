using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CursoTracker.Api.Domain;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Recommendations;

/// <summary>
/// Gemini com a ferramenta google_search (cota gratuita do Google AI Studio).
/// A busca do Google não combina com saída estruturada, então o modelo devolve um bloco JSON no texto.
/// </summary>
public sealed class GeminiCourseFinder(
    HttpClient http,
    IOptions<AiOptions> options,
    IConfiguration config,
    TimeProvider time,
    ILogger<GeminiCourseFinder> logger) : ICourseFinder
{
    public const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/";

    const string SystemPrompt = CourseSearchPrompt.Rules + """

        Responda somente com um bloco ```json contendo um objeto neste formato, com 4 a 8 cursos do mais para o
        menos indicado:
        {"courses":[{"name":"","platform":"","url":"","area":"","estimatedHours":0,"language":"","certificate":"","reason":""}],"note":""}
        Em "certificate" diga como o certificado gratuito é emitido; em "reason", em uma frase, por que combina
        com o perfil; em "note", uma observação curta (ou vazio). Se não encontrar opções confiáveis, devolva
        "courses" vazio e explique em "note".
        """;

    public async Task<CourseSearchResult> FindAsync(
        UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls, CancellationToken ct)
    {
        var apiKey = config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new CourseSearchException("A busca com IA ainda não foi configurada neste servidor.");

        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = CourseSearchPrompt.DescribeRequest(profile, refinement, knownUrls, time.GetUtcNow()) } } },
            },
            tools = new[] { new { google_search = new { } } },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}{options.Value.GeminiModel}:generateContent")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException e)
        {
            throw new CourseSearchException("A busca com IA está indisponível agora. Tente de novo em alguns minutos.", e);
        }

        using (response)
        {
            var payload = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new CourseSearchException("A cota gratuita da IA acabou por hoje. Tente de novo amanhã.");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini respondeu {Status}: {Body}", (int)response.StatusCode, payload);
                throw new CourseSearchException(DescribeError(response.StatusCode, payload));
            }
            return Parse(payload);
        }
    }

    // Erros de configuração do servidor ganham mensagem própria para dar para corrigir sem abrir os logs
    string DescribeError(HttpStatusCode status, string payload)
    {
        if (payload.Contains("API_KEY_INVALID", StringComparison.Ordinal) || payload.Contains("API key not valid", StringComparison.Ordinal))
            return "A chave do Gemini configurada no servidor é inválida (Gemini__ApiKey).";
        return status switch
        {
            HttpStatusCode.Forbidden => "A chave do Gemini não tem permissão para usar a API (Gemini__ApiKey).",
            HttpStatusCode.NotFound => $"O modelo \"{options.Value.GeminiModel}\" não está disponível no Gemini (Ai__GeminiModel).",
            _ => $"A busca com IA está indisponível agora (erro {(int)status}). Tente de novo em alguns minutos.",
        };
    }

    CourseSearchResult Parse(string payload)
    {
        var root = JsonNode.Parse(payload);
        var candidate = root?["candidates"]?[0];
        var finish = (string?)candidate?["finishReason"];
        var text = string.Concat(candidate?["content"]?["parts"]?.AsArray().Select(p => (string?)p?["text"]) ?? []);
        logger.LogInformation("Busca Gemini: fim={Finish} tokens={Tokens}", finish, (int?)root?["usageMetadata"]?["totalTokenCount"]);

        if (string.IsNullOrWhiteSpace(text))
            throw new CourseSearchException(finish is "SAFETY" or "PROHIBITED_CONTENT"
                ? "A IA não conseguiu atender esse pedido. Tente descrever o que procura de outro jeito."
                : "A IA não respondeu desta vez. Tente de novo.");

        var json = ExtractJson(text);
        if (json is null)
            return new CourseSearchResult([], text.Trim());

        try
        {
            var report = JsonSerializer.Deserialize<Report>(json, JsonSerializerOptions.Web);
            return new CourseSearchResult(report?.Courses ?? [], string.IsNullOrWhiteSpace(report?.Note) ? null : report.Note);
        }
        catch (JsonException e)
        {
            throw new CourseSearchException("A IA devolveu uma resposta inválida. Tente de novo.", e);
        }
    }

    /// <summary>Pega o JSON do bloco ```json (ou o primeiro objeto { ... } do texto).</summary>
    public static string? ExtractJson(string text)
    {
        var fence = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (fence >= 0)
        {
            var start = fence + "```json".Length;
            var end = text.IndexOf("```", start, StringComparison.Ordinal);
            return (end < 0 ? text[start..] : text[start..end]).Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        return open >= 0 && close > open ? text[open..(close + 1)] : null;
    }

    sealed record Report(List<CourseSuggestion>? Courses, string? Note);
}
