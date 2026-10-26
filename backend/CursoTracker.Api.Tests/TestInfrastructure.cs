using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Cursos;
using CursoTracker.Api.Features.Recommendations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CursoTracker.Api.Tests;

/// <summary>Um PostgreSQL por execução de testes; cada teste ganha um banco novo dentro dele.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string NewDatabase() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = $"t{Guid.NewGuid():N}" }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

public sealed class FakeCourseFinder : ICourseFinder
{
    public CourseSearchResult Result { get; set; } = new([], null);
    public Exception? Failure { get; set; }
    public List<(UserProfile Profile, string? Refinement, IReadOnlyCollection<string> KnownUrls)> Calls { get; } = [];

    public Task<CourseSearchResult> FindAsync(UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls, CancellationToken ct)
    {
        Calls.Add((profile, refinement, knownUrls));
        return Failure is null ? Task.FromResult(Result) : Task.FromException<CourseSearchResult>(Failure);
    }
}

public sealed class ApiFactory(string connectionString, int dailySearchLimit = 5) : WebApplicationFactory<Program>
{
    public const string InviteCode = "convite-teste";
    public const string Password = "senha1234";

    public FakeCourseFinder Finder { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Auth:InviteCode", InviteCode);
        builder.UseSetting("Ai:DailySearchLimit", dailySearchLimit.ToString());
        builder.ConfigureTestServices(services => services.AddSingleton<ICourseFinder>(Finder));
    }

    public async Task<HttpClient> CreateUserAsync(string email = "bruno@example.com")
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password, inviteCode = InviteCode });
        response.EnsureSuccessStatusCode();
        return client;
    }
}

public static class TestApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static CourseDetailsRequest Course(string date, string name = "Curso", double hours = 2, string url = "") =>
        new(DateOnly.Parse(date), "Plataforma", name, "Cloud", url == "" ? $"https://example.com/{Uri.EscapeDataString(name)}" : url, hours);

    public static async Task<CourseResponse> AddCourseAsync(this HttpClient client, CourseDetailsRequest course)
    {
        var response = await client.PostAsJsonAsync("/api/courses", course, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseResponse>(Json))!;
    }

    public static Task<List<CourseResponse>?> CoursesAsync(this HttpClient client) =>
        client.GetFromJsonAsync<List<CourseResponse>>("/api/courses", Json);

    public static async Task SaveProfileAsync(this HttpClient client, string lookingFor = "Cloud e DevOps para conseguir emprego")
    {
        var response = await client.PutAsJsonAsync("/api/profile",
            new { lookingFor, goal = "GetJob", level = "Beginner", language = "Portuguese", hoursPerWeek = 6 }, Json);
        response.EnsureSuccessStatusCode();
    }
}
