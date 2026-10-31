using System.Net;
using System.Net.Http.Json;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Premium;
using CursoTracker.Api.Features.Profile;
using CursoTracker.Api.Features.Recommendations;

namespace CursoTracker.Api.Tests;

[Collection(PostgresCollection.Name)]
public class PremiumTests(PostgresFixture db)
{
    static CourseSearchResult Found(int count) => new(
        [.. Enumerable.Range(1, count).Select(i => new CourseSuggestion(
            $"Curso {i}", "Plataforma", $"https://example.com/curso-{i}", "Cloud", 4, "Português", "Certificado gratuito", "Combina com você"))],
        null);

    static async Task<RecommendationResponse> SearchAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/recommendations", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecommendationResponse>(TestApi.Json))!;
    }

    static async Task<string> GenerateCodeAsync(HttpClient admin, int? days)
    {
        var response = await admin.PostAsJsonAsync("/api/premium/codes", new { days });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedCodeResponse>(TestApi.Json))!.Code;
    }

    [Fact]
    public async Task Free_plan_unlocks_three_courses_per_month_and_hides_the_rest()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        await client.SaveProfileAsync();
        factory.Finder.Result = Found(5);

        var first = await SearchAsync(client);

        Assert.Equal([false, false, false, true, true], first.Courses.Select(c => c.Locked));
        var locked = first.Courses[3];
        Assert.Equal(("", "", "", ""), (locked.Name, locked.Url, locked.Reason, locked.Certificate));
        Assert.Equal("Cloud", locked.Area);
        Assert.Equal(0, first.FreeCoursesLeft);

        var latest = await client.GetFromJsonAsync<RecommendationResponse>("/api/recommendations/latest", TestApi.Json);
        Assert.Equal(3, latest!.Courses.Count(c => !c.Locked));

        // Sem cursos grátis no mês, nem chama a IA
        var second = await client.PostAsJsonAsync("/api/recommendations", new { });
        Assert.Equal(HttpStatusCode.PaymentRequired, second.StatusCode);
        Assert.Contains("Você já liberou os 3 cursos grátis deste mês.", await second.Content.ReadAsStringAsync());
        Assert.Single(factory.Finder.Calls);
        Assert.Equal(4, (await client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestApi.Json))!.RemainingSearchesToday);
    }

    [Fact]
    public async Task Monthly_quota_is_shared_between_searches()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        await client.SaveProfileAsync();
        factory.Finder.Result = Found(2);
        await SearchAsync(client);

        factory.Finder.Result = new CourseSearchResult([.. Found(4).Courses.Select(c => c with { Url = c.Url + "-b" })], null);
        var second = await SearchAsync(client);

        Assert.Equal([false, true, true, true], second.Courses.Select(c => c.Locked));
    }

    [Fact]
    public async Task Premium_sees_every_course()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreatePremiumUserAsync();
        await client.SaveProfileAsync();
        factory.Finder.Result = Found(6);

        var result = await SearchAsync(client);

        Assert.All(result.Courses, c => Assert.False(c.Locked));
        Assert.Null(result.FreeCoursesLeft);
        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestApi.Json);
        Assert.Equal((null, null), (profile!.FreeCoursesLeft, profile.TopicLimit));
    }

    [Fact]
    public async Task Free_plan_allows_two_topics_and_premium_unlimited()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var free = await factory.CreateUserAsync("free@example.com");
        var premium = await factory.CreatePremiumUserAsync("premium@example.com");

        await free.SaveProfileAsync("C#", "SQL");
        var third = await free.SaveProfileResponseAsync(["C#", "SQL", "Azure"]);
        Assert.Equal(HttpStatusCode.PaymentRequired, third.StatusCode);
        Assert.Contains("até 2 assuntos", await third.Content.ReadAsStringAsync());
        Assert.Equal(2, (await free.GetFromJsonAsync<ProfileResponse>("/api/profile", TestApi.Json))!.TopicLimit);

        await premium.SaveProfileAsync("C#", "SQL", "Azure", "Docker");
        Assert.Equal(4, (await premium.GetFromJsonAsync<ProfileResponse>("/api/profile", TestApi.Json))!.Topics.Count);
    }

    [Fact]
    public async Task Search_sends_every_topic_to_the_ai()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        await client.SaveProfileAsync("C#", "SQL");

        await SearchAsync(client);

        Assert.Equal(["C#", "SQL"], Assert.Single(factory.Finder.Calls).Profile.Topics);
    }

    [Fact]
    public async Task Admin_generates_a_single_use_code_that_activates_premium()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var admin = await factory.CreateUserAsync(ApiFactory.AdminEmail);
        var ana = await factory.CreateUserAsync("ana@example.com");
        var bia = await factory.CreateUserAsync("bia@example.com");
        Assert.True((await admin.GetFromJsonAsync<MeResponse>("/api/auth/me", TestApi.Json))!.IsAdmin);

        var code = await GenerateCodeAsync(admin, days: 30);
        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", code);

        var redeemed = await ana.PostAsJsonAsync("/api/premium/redeem", new { code = $" {code.ToLowerInvariant().Replace("-", "")} " });
        var status = await redeemed.Content.ReadFromJsonAsync<PremiumStatusResponse>(TestApi.Json);
        Assert.True(status!.IsPremium);
        Assert.False(status.Forever);
        Assert.InRange(status.PremiumUntil!.Value, DateTimeOffset.UtcNow.AddDays(29), DateTimeOffset.UtcNow.AddDays(31));
        Assert.True((await ana.GetFromJsonAsync<MeResponse>("/api/auth/me", TestApi.Json))!.IsPremium);

        var reused = await bia.PostAsJsonAsync("/api/premium/redeem", new { code });
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Contains("Código inválido ou já usado.", await reused.Content.ReadAsStringAsync());

        var codes = await admin.GetFromJsonAsync<List<PremiumCodeResponse>>("/api/premium/codes", TestApi.Json);
        var listed = Assert.Single(codes!);
        Assert.Equal(("ana@example.com", code[..4]), (listed.RedeemedByEmail, listed.Hint));
    }

    [Fact]
    public async Task Lifetime_code_and_extension()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var admin = await factory.CreateUserAsync(ApiFactory.AdminEmail);
        var ana = await factory.CreateUserAsync("ana@example.com");

        await ana.PostAsJsonAsync("/api/premium/redeem", new { code = await GenerateCodeAsync(admin, days: 10) });
        var extended = await ana.PostAsJsonAsync("/api/premium/redeem", new { code = await GenerateCodeAsync(admin, days: 10) });
        var status = await extended.Content.ReadFromJsonAsync<PremiumStatusResponse>(TestApi.Json);
        Assert.InRange(status!.PremiumUntil!.Value, DateTimeOffset.UtcNow.AddDays(19), DateTimeOffset.UtcNow.AddDays(21));

        var lifetime = await ana.PostAsJsonAsync("/api/premium/redeem", new { code = await GenerateCodeAsync(admin, days: null) });
        var forever = await lifetime.Content.ReadFromJsonAsync<PremiumStatusResponse>(TestApi.Json);
        Assert.True(forever!.Forever);
        Assert.Equal("WhatsApp (11) 90000-0000", forever.Contact);
    }

    [Fact]
    public async Task Only_admins_manage_codes()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/premium/codes", new { days = 30 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/premium/codes")).StatusCode);
        Assert.False((await client.GetFromJsonAsync<PremiumStatusResponse>("/api/premium", TestApi.Json))!.IsPremium);
    }
}
