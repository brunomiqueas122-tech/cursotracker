using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using CursoTracker.Api.Domain;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Recommendations;

/// <summary>
/// Claude pesquisa na web (web search do lado da Anthropic) e devolve os cursos chamando
/// a ferramenta report_courses, cujo schema estrito garante JSON válido.
/// </summary>
public sealed class ClaudeCourseFinder(
    AnthropicClient client,
    IOptions<AiOptions> options,
    TimeProvider time,
    ILogger<ClaudeCourseFinder> logger) : ICourseFinder
{
    const string ReportTool = "report_courses";
    const int MaxContinuations = 3;

    const string SystemPrompt = CourseSearchPrompt.Rules + """

        Ao terminar, chame a ferramenta report_courses uma única vez com 4 a 8 cursos, do mais para o menos
        indicado. Se não encontrar opções confiáveis, chame report_courses com a lista vazia e explique o motivo em note.
        """;

    public async Task<CourseSearchResult> FindAsync(
        UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls, CancellationToken ct)
    {
        var opts = options.Value;
        List<BetaMessageParam> messages = [new BetaUserMessageParam { Content = CourseSearchPrompt.DescribeRequest(profile, refinement, knownUrls, time.GetUtcNow()) }];

        for (var turn = 0; turn <= MaxContinuations; turn++)
        {
            BetaMessage response;
            try
            {
                response = await client.Beta.Messages.Create(new MessageCreateParams
                {
                    Model = opts.ClaudeModel,
                    MaxTokens = 16000,
                    // Se o modelo recusar por engano, a própria API tenta de novo com um modelo de reserva
                    Betas = ["server-side-fallback-2026-07-01"],
                    Fallbacks = new BetaFallbacksParam(JsonSerializer.SerializeToElement("default")),
                    System = SystemPrompt,
                    OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
                    Tools =
                    [
                        new BetaToolUnion(new BetaWebSearchTool20260209 { MaxUses = opts.MaxWebSearches }),
                        new BetaToolUnion(ReportCoursesTool),
                    ],
                    Messages = messages,
                }, ct);
            }
            catch (AnthropicApiException e)
            {
                throw new CourseSearchException("A busca com IA está indisponível agora. Tente de novo em alguns minutos.", e);
            }

            logger.LogInformation(
                "Busca IA: stop={StopReason} entrada={InputTokens} saída={OutputTokens}",
                response.StopReason, response.Usage.InputTokens, response.Usage.OutputTokens);

            foreach (var block in response.Content)
            {
                if (block.TryPickToolUse(out var toolUse) && toolUse.Name == ReportTool)
                    return ParseReport(toolUse.Input);
            }

            if (response.StopReason == "pause_turn")
            {
                // A pesquisa no servidor parou no meio: devolve o que veio para o modelo continuar de onde estava
                messages.Add(new BetaAssistantMessageParam { Content = ToParams(response.Content) });
                continue;
            }
            if (response.StopReason == "refusal")
                throw new CourseSearchException("A IA não conseguiu atender esse pedido. Tente descrever o que procura de outro jeito.");

            var text = string.Join("\n", response.Content.Select(b => b.TryPickText(out var t) ? t.Text : null).OfType<string>()).Trim();
            return new CourseSearchResult([], text.Length > 0 ? text : "Não encontrei cursos desta vez. Tente ajustar o pedido.");
        }

        throw new CourseSearchException("A busca demorou demais. Tente um pedido mais específico.");
    }

    static readonly BetaTool ReportCoursesTool = new()
    {
        Name = ReportTool,
        Description = "Entrega a lista final de cursos gratuitos com certificado gratuito encontrados para a pessoa.",
        Strict = true,
        // Montado em JSON: o modo estrito exige additionalProperties=false, que o tipo do SDK não expõe
        InputSchema = JsonSerializer.Deserialize<InputSchema>(JsonSerializer.Serialize(new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "courses", "note" },
            properties = new
            {
                courses = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "name", "platform", "url", "area", "estimatedHours", "language", "certificate", "reason" },
                        properties = new
                        {
                            name = new { type = "string", description = "Nome oficial do curso" },
                            platform = new { type = "string", description = "Instituição ou plataforma" },
                            url = new { type = "string", description = "Link oficial da página do curso" },
                            area = new { type = "string", description = "Área curta, ex.: Cloud, Dados, Segurança" },
                            estimatedHours = new { type = "number", description = "Carga horária estimada em horas" },
                            language = new { type = "string", description = "Idioma do curso" },
                            certificate = new { type = "string", description = "Como o certificado gratuito é emitido" },
                            reason = new { type = "string", description = "Por que combina com o perfil, em uma frase" },
                        },
                    },
                },
                note = new { type = "string", description = "Observação curta para a pessoa; vazio se não houver" },
            },
        }))!,
    };

    static CourseSearchResult ParseReport(IReadOnlyDictionary<string, JsonElement> input)
    {
        try
        {
            var json = JsonSerializer.Serialize(input);
            var report = JsonSerializer.Deserialize<Report>(json, JsonSerializerOptions.Web)
                         ?? throw new JsonException("Relatório vazio.");
            return new CourseSearchResult(report.Courses ?? [], string.IsNullOrWhiteSpace(report.Note) ? null : report.Note);
        }
        catch (JsonException e)
        {
            throw new CourseSearchException("A IA devolveu uma resposta inválida. Tente de novo.", e);
        }
    }

    sealed record Report(List<CourseSuggestion>? Courses, string? Note);

    static List<BetaContentBlockParam> ToParams(IReadOnlyList<BetaContentBlock> content) =>
        content.Select(b => new BetaContentBlockParam(b.Json)).ToList();
}
