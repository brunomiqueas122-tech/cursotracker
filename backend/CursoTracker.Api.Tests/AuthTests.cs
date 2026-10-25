using System.Net;
using System.Net.Http.Json;

namespace CursoTracker.Api.Tests;

public class AuthTests
{
    [Theory]
    [InlineData("/api/courses")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/auth/me")]
    public async Task Protected_endpoints_require_login(string url)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { password = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/auth/me")).StatusCode);

        await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Repeated_login_attempts_are_rate_limited()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        HttpResponseMessage last = null!;
        for (var i = 0; i < 6; i++)
            last = await client.PostAsJsonAsync("/api/auth/login", new { password = "errada" });

        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
    }
}
