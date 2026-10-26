using System.Net;
using System.Net.Http.Json;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Profile;
using CursoTracker.Api.Features.Recommendations;

namespace CursoTracker.Api.Tests;

[Collection(PostgresCollection.Name)]
public class RecommendationTests(PostgresFixture db)
{
    static CourseSuggestion Suggestion(string name, string url, double hours = 4) =>
        new(name, "Plataforma", url, "Cloud", hours, "Português", "Certificado gratuito ao concluir", "Combina com seu objetivo");

    [Fact]
    public async Task Profile_is_created_and_marks_onboarding_done()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/profile")).StatusCode);

        await client.SaveProfileAsync("Quero aprender Kubernetes");

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestApi.Json);
        Assert.Equal("Quero aprender Kubernetes", profile!.LookingFor);
        Assert.Equal(5, profile.RemainingSearchesToday);
        Assert.True((await client.GetFromJsonAsync<MeResponse>("/api/auth/me", TestApi.Json))!.HasProfile);
    }

    [Fact]
    public async Task Profile_answers_are_validated()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/profile",
            new { lookingFor = "", goal = "GetJob", level = "Beginner", language = "Any", hoursPerWeek = 0 }, TestApi.Json);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Conte que tipo de curso você está procurando.", body);
        Assert.Contains("Informe entre 1 e 80 horas por semana.", body);
    }

    [Fact]
    public async Task Searching_requires_a_profile()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/recommendations", new { refinement = (string?)null });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Search_uses_the_profile_and_skips_unsafe_or_repeated_results()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        await client.SaveProfileAsync();
        await client.AddCourseAsync(TestApi.Course("2026-10-25", "Já tenho", url: "https://ja-tenho.example.com/curso"));
        factory.Finder.Result = new CourseSearchResult(
        [
            Suggestion("Bom", "https://bom.example.com/curso", hours: 3.3),
            Suggestion("Repetido", "https://ja-tenho.example.com/curso/"),
            Suggestion("Perigoso", "javascript:alert(1)"),
            Suggestion("Bom de novo", "https://bom.example.com/curso"),
        ], "Encontrei poucas opções em português.");

        var response = await client.PostAsJsonAsync("/api/recommendations", new { refinement = "  só em português  " });

        var result = await response.Content.ReadFromJsonAsync<RecommendationResponse>(TestApi.Json);
        Assert.Equal(["Bom"], result!.Courses.Select(c => c.Name));
        Assert.Equal(3.5, result.Courses[0].EstimatedHours);
        Assert.Equal("Encontrei poucas opções em português.", result.Note);
        Assert.Equal(4, result.RemainingSearchesToday);

        var call = Assert.Single(factory.Finder.Calls);
        Assert.Equal("só em português", call.Refinement);
        Assert.Equal(LearningGoal.GetJob, call.Profile.Goal);
        Assert.Contains("https://ja-tenho.example.com/curso", call.KnownUrls);
    }

    [Fact]
    public async Task Daily_limit_blocks_extra_searches()
    {
        await using var factory = new ApiFactory(db.NewDatabase(), dailySearchLimit: 2);
        var client = await factory.CreateUserAsync();
        await client.SaveProfileAsync();

        await client.PostAsJsonAsync("/api/recommendations", new { });
        await client.PostAsJsonAsync("/api/recommendations", new { });
        var third = await client.PostAsJsonAsync("/api/recommendations", new { });

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Contains("Você já fez as 2 buscas de hoje.", await third.Content.ReadAsStringAsync());
        Assert.Equal(2, factory.Finder.Calls.Count);
    }

    [Fact]
    public async Task Failed_search_does_not_use_up_the_limit()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        await client.SaveProfileAsync();
        factory.Finder.Failure = new CourseSearchException("A busca com IA está indisponível agora.");

        var response = await client.PostAsJsonAsync("/api/recommendations", new { });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("A busca com IA está indisponível agora.", await response.Content.ReadAsStringAsync());
        Assert.Equal(5, (await client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestApi.Json))!.RemainingSearchesToday);
    }
}
