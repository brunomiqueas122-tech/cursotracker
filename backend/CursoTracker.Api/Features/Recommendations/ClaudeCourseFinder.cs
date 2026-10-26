using System.Text;
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

    const string SystemPrompt = """
        Você ajuda brasileiros a encontrar cursos online gratuitos que dão certificado gratuito.

        Use a busca na web para encontrar opções atuais. Só inclua um curso depois de confirmar, em uma página
        oficial do provedor, que o curso e o certificado são gratuitos. Descarte cursos em que só o conteúdo é
        grátis e o certificado é pago (como o modo "auditar" de algumas plataformas), cursos com teste grátis
        que depois cobram, e páginas de revenda ou agregadores duvidosos.

        Escolha os cursos pelo perfil da pessoa: o que ela procura, objetivo, nível, idioma preferido e tempo
        disponível. Prefira provedores reconhecidos pelo mercado. Não sugira cursos cujo link já está no plano dela.

        Ao terminar, chame a ferramenta report_courses uma única vez com 4 a 8 cursos, do mais para o menos
        indicado. Escreva os textos em português do Brasil. Se não encontrar opções confiáveis, chame
        report_courses com a lista vazia e explique o motivo em note.
        """;

    public async Task<CourseSearchResult> FindAsync(
        UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls, CancellationToken ct)
    {
        var opts = options.Value;
        List<BetaMessageParam> messages = [new BetaUserMessageParam { Content = DescribeRequest(profile, refinement, knownUrls) }];

        for (var turn = 0; turn <= MaxContinuations; turn++)
        {
            BetaMessage response;
            try
            {
                response = await client.Beta.Messages.Create(new MessageCreateParams
                {
                    Model = opts.Model,
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

    string DescribeRequest(UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Data de hoje: {time.GetUtcNow():yyyy-MM-dd}");
        sb.AppendLine($"O que procuro: {profile.LookingFor}");
        sb.AppendLine($"Objetivo: {Describe(profile.Goal)}");
        sb.AppendLine($"Nível: {Describe(profile.Level)}");
        sb.AppendLine($"Idioma dos cursos: {Describe(profile.Language)}");
        sb.AppendLine($"Tempo disponível: {profile.HoursPerWeek} horas por semana");
        if (!string.IsNullOrWhiteSpace(refinement))
            sb.AppendLine($"Ajuste pedido nesta busca: {refinement}");
        if (knownUrls.Count > 0)
        {
            sb.AppendLine("Cursos que já estão no meu plano (não repita):");
            foreach (var url in knownUrls.Take(100))
                sb.AppendLine($"- {url}");
        }
        return sb.ToString();
    }

    static string Describe(LearningGoal goal) => goal switch
    {
        LearningGoal.GetJob => "conseguir um emprego",
        LearningGoal.GrowInCurrentJob => "crescer no trabalho atual",
        LearningGoal.ChangeCareer => "mudar de área",
        _ => "aprender por interesse pessoal",
    };

    static string Describe(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => "iniciante",
        SkillLevel.Intermediate => "intermediário",
        _ => "avançado",
    };

    static string Describe(CourseLanguage language) => language switch
    {
        CourseLanguage.Portuguese => "português",
        CourseLanguage.English => "inglês",
        _ => "português ou inglês",
    };

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
