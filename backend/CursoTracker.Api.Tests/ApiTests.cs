using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Cursos;
using CursoTracker.Api.Features.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace CursoTracker.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    readonly string _connectionString = $"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    readonly SqliteConnection _keepAlive;

    public ApiFactory()
    {
        // Banco em memória some quando a última conexão fecha; esta segura ele durante o teste
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    public const string Password = "senha-de-teste";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Auth:Password", Password);
    }

    public async Task<HttpClient> CreateLoggedInClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { password = Password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}

public class ApiTests
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    static UpdateProgressRequest Request(double hours, CourseStatus status, bool cert = false, string? certUrl = null) =>
        new(hours, status, cert, certUrl, OnLinkedIn: false, Notes: null);

    [Fact]
    public async Task Seed_loads_all_courses_ordered_by_day()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();

        var courses = await client.GetFromJsonAsync<List<CourseResponse>>("/api/courses", Json, CancellationToken.None);

        Assert.NotNull(courses);
        Assert.Equal(11, courses.Count);
        Assert.Equal("Developing Secure Software (LFD121) – módulos 1 a 4", courses[0].Name);
        Assert.Equal(courses.Select(c => c.Day).Order(), courses.Select(c => c.Day));
    }

    [Fact]
    public async Task Completing_a_course_updates_course_and_dashboard()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();
        var ct = CancellationToken.None;

        var response = await client.PutAsJsonAsync("/api/courses/1/progress",
            Request(5, CourseStatus.Completed, cert: true, certUrl: "https://cert.example.com/1"), Json, ct);

        response.EnsureSuccessStatusCode();
        var course = await response.Content.ReadFromJsonAsync<CourseResponse>(Json, ct);
        Assert.NotNull(course?.CompletedAt);

        var dashboard = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard", Json, ct);
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.CompletedCourses);
        Assert.Equal(9.1, dashboard.CompletedPercent);
        Assert.Equal(1, dashboard.CertificatesReceived);
        Assert.Equal(5, dashboard.HoursDone);
        Assert.Equal(35, dashboard.EstimatedHours);

        var completed = await client.GetFromJsonAsync<List<CourseResponse>>("/api/courses?status=Completed", Json, ct);
        Assert.Single(completed!);
    }

    [Fact]
    public async Task Negative_hours_return_validation_problem()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();
        var ct = CancellationToken.None;

        var response = await client.PutAsJsonAsync("/api/courses/1/progress", Request(-2, CourseStatus.InProgress), Json, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        Assert.Contains("HoursDone", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Invalid_certificate_url_returns_validation_problem()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();
        var ct = CancellationToken.None;

        var response = await client.PutAsJsonAsync("/api/courses/1/progress",
            Request(5, CourseStatus.Completed, cert: true, certUrl: "javascript:alert(1)"), Json, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_course_returns_404()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/courses/999/progress", Request(1, CourseStatus.InProgress), Json,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
