using System.Net;
using System.Net.Http.Json;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Cursos;
using CursoTracker.Api.Features.Dashboard;

namespace CursoTracker.Api.Tests;

[Collection(PostgresCollection.Name)]
public class CourseApiTests(PostgresFixture db)
{
    static UpdateProgressRequest Progress(double hours, CourseStatus status, bool cert = false, string? certUrl = null) =>
        new(hours, status, cert, certUrl, OnLinkedIn: false, Notes: null);

    [Fact]
    public async Task Plan_days_follow_the_dates()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();

        await client.AddCourseAsync(TestApi.Course("2026-10-25", "A"));
        var later = await client.AddCourseAsync(TestApi.Course("2026-11-02", "B"));

        Assert.Equal(9, later.Day);
    }

    [Fact]
    public async Task Rescheduling_before_the_start_renumbers_the_plan()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        var first = await client.AddCourseAsync(TestApi.Course("2026-10-25", "A"));
        var second = await client.AddCourseAsync(TestApi.Course("2026-10-27", "B"));

        var response = await client.PutAsJsonAsync($"/api/courses/{second.Id}", TestApi.Course("2026-10-24", "B"), TestApi.Json);

        response.EnsureSuccessStatusCode();
        var courses = (await client.CoursesAsync())!;
        Assert.Equal(1, courses.Single(c => c.Id == second.Id).Day);
        Assert.Equal(2, courses.Single(c => c.Id == first.Id).Day);
    }

    [Fact]
    public async Task Completing_a_course_updates_the_dashboard()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        var course = await client.AddCourseAsync(TestApi.Course("2026-10-25", "A", hours: 5));
        await client.AddCourseAsync(TestApi.Course("2026-10-26", "B", hours: 3));

        var response = await client.PutAsJsonAsync($"/api/courses/{course.Id}/progress",
            Progress(5, CourseStatus.Completed, cert: true, certUrl: "https://cert.example.com/1"), TestApi.Json);

        response.EnsureSuccessStatusCode();
        var dashboard = await client.GetFromJsonAsync<DashboardSummary>("/api/dashboard", TestApi.Json);
        Assert.Equal(50, dashboard!.CompletedPercent);
        Assert.Equal(1, dashboard.CertificatesReceived);
        Assert.Equal(8, dashboard.EstimatedHours);
    }

    [Fact]
    public async Task Invalid_input_returns_validation_problems()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        var course = await client.AddCourseAsync(TestApi.Course("2026-10-25", "A"));

        var badCourse = await client.PostAsJsonAsync("/api/courses", TestApi.Course("2026-10-25", "", hours: 0), TestApi.Json);
        var badProgress = await client.PutAsJsonAsync($"/api/courses/{course.Id}/progress",
            Progress(5, CourseStatus.Completed, cert: true, certUrl: "javascript:alert(1)"), TestApi.Json);

        Assert.Equal(HttpStatusCode.BadRequest, badCourse.StatusCode);
        Assert.Contains("Informe o nome do curso.", await badCourse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, badProgress.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_course_removes_it()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        var course = await client.AddCourseAsync(TestApi.Course("2026-10-25", "A"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/courses/{course.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/courses/{course.Id}")).StatusCode);
    }

    [Fact]
    public async Task Importing_the_phone_backup_keeps_progress()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();
        // Formato do arquivo "Baixar backup" do app antigo (campos extras são ignorados)
        const string backup = """
            {"version":1,"courses":[
              {"id":1,"day":1,"date":"2026-10-25","platform":"Linux Foundation","name":"LFD121","area":"Segurança",
               "url":"https://training.linuxfoundation.org/lfd121","estimatedHours":5,"hoursDone":5,"status":"Completed",
               "completedAt":"2026-10-26T20:00:00Z","certificateReceived":true,"certificateUrl":"https://cert.example.com/1",
               "onLinkedIn":true,"notes":"ótimo"},
              {"id":2,"day":3,"date":"2026-10-27","platform":"AWS","name":"Cloud Practitioner","area":"Cloud",
               "url":"https://skillbuilder.aws/","estimatedHours":5,"hoursDone":0,"status":"NotStarted","completedAt":null,
               "certificateReceived":false,"certificateUrl":null,"onLinkedIn":false,"notes":null}
            ]}
            """;

        var response = await client.PostAsync("/api/courses/import", new StringContent(backup, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(new ImportResponse(2), await response.Content.ReadFromJsonAsync<ImportResponse>(TestApi.Json));
        var courses = (await client.CoursesAsync())!;
        Assert.Equal([1, 3], courses.Select(c => c.Day));
        Assert.Equal(DateTimeOffset.Parse("2026-10-26T20:00:00Z"), courses[0].CompletedAt);
        Assert.True(courses[0].OnLinkedIn);
    }

    [Fact]
    public async Task Import_rejects_invalid_files()
    {
        await using var factory = new ApiFactory(db.NewDatabase());
        var client = await factory.CreateUserAsync();

        var empty = await client.PostAsJsonAsync("/api/courses/import", new { courses = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Contains("O arquivo não tem cursos.", await empty.Content.ReadAsStringAsync());
    }
}
