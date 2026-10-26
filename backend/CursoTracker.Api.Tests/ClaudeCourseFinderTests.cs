using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Tests;

/// <summary>API da Anthropic simulada: guarda cada requisição e responde a próxima resposta da fila.</summary>
sealed class FakeAnthropicHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
{
    readonly Queue<(HttpStatusCode Status, string Body)> _responses = new(responses);
    public List<(JsonNode Body, HttpRequestMessage Request)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add((JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!, request));
        var (status, body) = _responses.Dequeue();
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}

public class ClaudeCourseFinderTests
{
    static readonly UserProfile Profile = new("user-1",
        new ProfileAnswers("Cloud e DevOps", LearningGoal.GetJob, SkillLevel.Beginner, CourseLanguage.Portuguese, 6));

    static string Message(string stopReason, params object[] content) => JsonSerializer.Serialize(new
    {
        id = "msg_1",
        type = "message",
        role = "assistant",
        model = "claude-opus-5-5",
        content,
        stop_reason = stopReason,
        stop_sequence = (string?)null,
        usage = new { input_tokens = 1200, output_tokens = 300 },
    });

    static object Report(params object[] courses) => new
    {
        type = "tool_use",
        id = "toolu_1",
        name = "report_courses",
        input = new { courses, note = "Todos confirmados nas páginas oficiais." },
    };

    static readonly object Kubernetes = new
    {
        name = "Introduction to Kubernetes",
        platform = "Linux Foundation",
        url = "https://training.linuxfoundation.org/training/introduction-to-kubernetes/",
        area = "DevOps",
        estimatedHours = 14,
        language = "Inglês",
        certificate = "Certificado de conclusão gratuito",
        reason = "Base de DevOps pedida em vagas de entrada.",
    };

    static (ClaudeCourseFinder Finder, FakeAnthropicHandler Api) Create(params (HttpStatusCode, string)[] responses)
    {
        var api = new FakeAnthropicHandler(responses);
        var client = new AnthropicClient { ApiKey = "test-key", HttpClient = new HttpClient(api), MaxRetries = 0 };
        var finder = new ClaudeCourseFinder(client, Options.Create(new AiOptions()), TimeProvider.System, NullLogger<ClaudeCourseFinder>.Instance);
        return (finder, api);
    }

    [Fact]
    public async Task Sends_web_search_strict_report_tool_and_profile()
    {
        var (finder, api) = Create((HttpStatusCode.OK, Message("tool_use", Report(Kubernetes))));

        await finder.FindAsync(Profile, "só gratuitos de verdade", ["https://ja-tenho.example.com"], CancellationToken.None);

        var (body, request) = Assert.Single(api.Requests);
        Assert.Equal("claude-opus-5-5", (string)body["model"]!);
        Assert.Equal("medium", (string)body["output_config"]!["effort"]!);
        Assert.Equal("default", (string)body["fallbacks"]!);
        Assert.Contains("server-side-fallback-2026-07-01", string.Join(",", request.Headers.GetValues("anthropic-beta")));

        var tools = body["tools"]!.AsArray();
        Assert.Equal("web_search_20260209", (string)tools[0]!["type"]!);
        Assert.Equal(6, (int)tools[0]!["max_uses"]!);
        Assert.Equal("report_courses", (string)tools[1]!["name"]!);
        Assert.True((bool)tools[1]!["strict"]!);
        Assert.False((bool)tools[1]!["input_schema"]!["additionalProperties"]!);

        var content = body["messages"]![0]!["content"]!;
        var prompt = content is JsonValue ? content.GetValue<string>() : content.ToJsonString();
        Assert.Contains("Cloud e DevOps", prompt);
        Assert.Contains("só gratuitos de verdade", prompt);
        Assert.Contains("https://ja-tenho.example.com", prompt);
    }

    [Fact]
    public async Task Reads_courses_from_the_report_tool()
    {
        var (finder, _) = Create((HttpStatusCode.OK, Message("tool_use",
            new { type = "text", text = "Pesquisei nas páginas oficiais." }, Report(Kubernetes))));

        var result = await finder.FindAsync(Profile, null, [], CancellationToken.None);

        var course = Assert.Single(result.Courses);
        Assert.Equal("Introduction to Kubernetes", course.Name);
        Assert.Equal(14, course.EstimatedHours);
        Assert.Equal("Todos confirmados nas páginas oficiais.", result.Note);
    }

    [Fact]
    public async Task Continues_when_the_server_side_search_pauses()
    {
        var (finder, api) = Create(
            (HttpStatusCode.OK, Message("pause_turn", new { type = "text", text = "Ainda pesquisando..." })),
            (HttpStatusCode.OK, Message("tool_use", Report(Kubernetes))));

        var result = await finder.FindAsync(Profile, null, [], CancellationToken.None);

        Assert.Single(result.Courses);
        var second = api.Requests[1].Body["messages"]!.AsArray();
        Assert.Equal(2, second.Count);
        Assert.Equal("assistant", (string)second[1]!["role"]!);
        Assert.Contains("Ainda pesquisando", second[1]!["content"]!.ToJsonString());
    }

    [Fact]
    public async Task Refusal_becomes_a_friendly_error()
    {
        var (finder, _) = Create((HttpStatusCode.OK, Message("refusal")));

        var e = await Assert.ThrowsAsync<CourseSearchException>(() => finder.FindAsync(Profile, null, [], CancellationToken.None));

        Assert.Contains("Tente descrever", e.Message);
    }

    [Fact]
    public async Task Api_errors_become_a_friendly_error()
    {
        var (finder, _) = Create((HttpStatusCode.InternalServerError,
            """{"type":"error","error":{"type":"api_error","message":"boom"}}"""));

        var e = await Assert.ThrowsAsync<CourseSearchException>(() => finder.FindAsync(Profile, null, [], CancellationToken.None));

        Assert.Contains("indisponível", e.Message);
    }

    [Fact]
    public async Task Answer_without_report_returns_the_text_as_note()
    {
        var (finder, _) = Create((HttpStatusCode.OK, Message("end_turn", new { type = "text", text = "Não achei cursos confiáveis." })));

        var result = await finder.FindAsync(Profile, null, [], CancellationToken.None);

        Assert.Empty(result.Courses);
        Assert.Equal("Não achei cursos confiáveis.", result.Note);
    }
}
