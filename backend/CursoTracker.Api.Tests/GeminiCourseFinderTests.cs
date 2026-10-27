using System.Net;
using System.Text.Json;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Recommendations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Tests;

public class GeminiCourseFinderTests
{
    static readonly UserProfile Profile = new("user-1",
        new ProfileAnswers("Cloud e DevOps", LearningGoal.GetJob, SkillLevel.Beginner, CourseLanguage.Portuguese, 6));

    const string CoursesJson = """
        {"courses":[{"name":"Introduction to Kubernetes","platform":"Linux Foundation",
          "url":"https://training.linuxfoundation.org/training/introduction-to-kubernetes/","area":"DevOps",
          "estimatedHours":14,"language":"Inglês","certificate":"Certificado de conclusão gratuito",
          "reason":"Base de DevOps pedida em vagas de entrada."}],
         "note":"Confirmado nas páginas oficiais."}
        """;

    static string Answer(string text, string finishReason = "STOP") => JsonSerializer.Serialize(new
    {
        candidates = new[] { new { content = new { role = "model", parts = new[] { new { text } } }, finishReason } },
        usageMetadata = new { totalTokenCount = 1500 },
    });

    static (GeminiCourseFinder Finder, FakeAnthropicHandler Api) Create(HttpStatusCode status, string body, string? apiKey = "gemini-key")
    {
        var api = new FakeAnthropicHandler((status, body));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = apiKey }).Build();
        var finder = new GeminiCourseFinder(new HttpClient(api), Options.Create(new AiOptions()), config, TimeProvider.System,
            NullLogger<GeminiCourseFinder>.Instance);
        return (finder, api);
    }

    [Fact]
    public async Task Sends_google_search_tool_profile_and_api_key()
    {
        var (finder, api) = Create(HttpStatusCode.OK, Answer($"```json\n{CoursesJson}\n```"));

        await finder.FindAsync(Profile, "só gratuitos de verdade", ["https://ja-tenho.example.com"], CancellationToken.None);

        var (body, request) = Assert.Single(api.Requests);
        Assert.Equal($"{GeminiCourseFinder.Endpoint}gemini-2.5-flash:generateContent", request.RequestUri!.ToString());
        Assert.Equal("gemini-key", Assert.Single(request.Headers.GetValues("x-goog-api-key")));
        Assert.NotNull(body["tools"]![0]!["google_search"]);
        Assert.Contains("certificado é pago", body["systemInstruction"]!["parts"]![0]!["text"]!.GetValue<string>());
        var prompt = body["contents"]![0]!["parts"]![0]!["text"]!.GetValue<string>();
        Assert.Contains("Cloud e DevOps", prompt);
        Assert.Contains("só gratuitos de verdade", prompt);
        Assert.Contains("https://ja-tenho.example.com", prompt);
    }

    [Fact]
    public async Task Reads_courses_from_the_json_block()
    {
        var (finder, _) = Create(HttpStatusCode.OK, Answer($"Encontrei estes cursos:\n```json\n{CoursesJson}\n```\nBons estudos!"));

        var result = await finder.FindAsync(Profile, null, [], CancellationToken.None);

        var course = Assert.Single(result.Courses);
        Assert.Equal("Introduction to Kubernetes", course.Name);
        Assert.Equal(14, course.EstimatedHours);
        Assert.Equal("Confirmado nas páginas oficiais.", result.Note);
    }

    [Fact]
    public async Task Accepts_json_without_the_code_fence()
    {
        var (finder, _) = Create(HttpStatusCode.OK, Answer(CoursesJson));

        Assert.Single((await finder.FindAsync(Profile, null, [], CancellationToken.None)).Courses);
    }

    [Fact]
    public async Task Answer_without_json_becomes_the_note()
    {
        var (finder, _) = Create(HttpStatusCode.OK, Answer("Não achei cursos confiáveis com esse filtro."));

        var result = await finder.FindAsync(Profile, null, [], CancellationToken.None);

        Assert.Empty(result.Courses);
        Assert.Equal("Não achei cursos confiáveis com esse filtro.", result.Note);
    }

    [Fact]
    public async Task Free_quota_exhausted_has_its_own_message()
    {
        var (finder, _) = Create(HttpStatusCode.TooManyRequests, """{"error":{"code":429,"status":"RESOURCE_EXHAUSTED"}}""");

        var e = await Assert.ThrowsAsync<CourseSearchException>(() => finder.FindAsync(Profile, null, [], CancellationToken.None));

        Assert.Contains("cota gratuita", e.Message);
    }

    [Fact]
    public async Task Other_api_errors_become_a_friendly_error()
    {
        var (finder, _) = Create(HttpStatusCode.BadRequest, """{"error":{"code":400,"message":"API key not valid"}}""");

        var e = await Assert.ThrowsAsync<CourseSearchException>(() => finder.FindAsync(Profile, null, [], CancellationToken.None));

        Assert.Contains("indisponível", e.Message);
    }

    [Fact]
    public async Task Missing_key_is_reported_without_calling_the_api()
    {
        var (finder, api) = Create(HttpStatusCode.OK, Answer(CoursesJson), apiKey: null);

        var e = await Assert.ThrowsAsync<CourseSearchException>(() => finder.FindAsync(Profile, null, [], CancellationToken.None));

        Assert.Contains("não foi configurada", e.Message);
        Assert.Empty(api.Requests);
    }

    [Fact]
    public async Task Blocked_answer_asks_to_rephrase()
    {
        var (finder, _) = Create(HttpStatusCode.OK, Answer("", finishReason: "SAFETY"));

        var e = await Assert.ThrowsAsync<CourseSearchException>(() => finder.FindAsync(Profile, null, [], CancellationToken.None));

        Assert.Contains("de outro jeito", e.Message);
    }
}
