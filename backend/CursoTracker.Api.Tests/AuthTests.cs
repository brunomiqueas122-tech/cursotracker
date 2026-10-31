using System.Net;
using System.Net.Http.Json;
using CursoTracker.Api.Features.Auth;

namespace CursoTracker.Api.Tests;

[Collection(PostgresCollection.Name)]
public class AuthTests(PostgresFixture db)
{
    [Theory]
    [InlineData("/api/courses")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/profile")]
    [InlineData("/api/auth/me")]
    public async Task Protected_endpoints_require_login(string url)
    {
        await using var factory = new ApiFactory(db.NewDatabase());

        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registering_logs_the_user_in()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync("ana@example.com");

        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me", TestApi.Json);

        Assert.Equal(new MeResponse("ana@example.com", HasProfile: false, IsPremium: false, IsAdmin: false), me);
    }

    [Fact]
    public async Task Registration_requires_the_invite_code()
    {
        await using var factory = new ApiFactory(db.NewDatabase());

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { email = "ana@example.com", password = ApiFactory.Password, inviteCode = "errado" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Código de convite inválido.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Registration_errors_are_in_portuguese()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        await factory.CreateUserAsync("ana@example.com");
        var client = factory.CreateClient();

        var duplicate = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "ana@example.com", password = ApiFactory.Password, inviteCode = ApiFactory.InviteCode });
        var weak = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "bia@example.com", password = "abc", inviteCode = ApiFactory.InviteCode });

        Assert.Contains("Já existe uma conta com esse e-mail.", await duplicate.Content.ReadAsStringAsync());
        var weakBody = await weak.Content.ReadAsStringAsync();
        Assert.Contains("A senha precisa ter pelo menos 8 caracteres.", weakBody);
        Assert.Contains("A senha precisa ter pelo menos um número.", weakBody);
    }

    [Fact]
    public async Task Login_and_logout()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        await factory.CreateUserAsync("ana@example.com");
        var client = factory.CreateClient();

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = "ana@example.com", password = "errada123" });
        var right = await client.PostAsJsonAsync("/api/auth/login", new { email = "ana@example.com", password = ApiFactory.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Repeated_login_attempts_are_rate_limited()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = factory.CreateClient();

        HttpResponseMessage last = null!;
        for (var i = 0; i < 11; i++)
            last = await client.PostAsJsonAsync("/api/auth/login", new { email = "x@example.com", password = "errada123" });

        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
    }

    [Fact]
    public async Task Users_only_see_their_own_courses()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var ana = await factory.CreateUserAsync("ana@example.com");
        var bia = await factory.CreateUserAsync("bia@example.com");
        var anaCourse = await ana.AddCourseAsync(TestApi.Course("2026-11-02", "Curso da Ana"));

        Assert.Empty((await bia.CoursesAsync())!);
        Assert.Equal(HttpStatusCode.NotFound, (await bia.GetAsync($"/api/courses/{anaCourse.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bia.DeleteAsync($"/api/courses/{anaCourse.Id}")).StatusCode);
        Assert.Single((await ana.CoursesAsync())!);
    }
}
