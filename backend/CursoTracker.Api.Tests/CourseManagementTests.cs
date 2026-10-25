using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursoTracker.Api.Features.Cursos;

namespace CursoTracker.Api.Tests;

public class CourseManagementTests
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    static CourseDetailsRequest Details(string date = "2026-11-02", string name = "Docker para iniciantes", double hours = 3) =>
        new(DateOnly.Parse(date), "Udemy", name, "DevOps", "https://www.udemy.com/course/docker", hours);

    [Fact]
    public async Task Creating_a_course_places_it_on_the_matching_day()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/courses", Details(), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CourseResponse>(Json);
        Assert.Equal(9, created!.Day); // 25/10 é o dia 1
        Assert.Equal(12, (await client.GetFromJsonAsync<List<CourseResponse>>("/api/courses", Json))!.Count);
    }

    [Fact]
    public async Task Rescheduling_before_the_start_renumbers_the_plan()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();
        var original = (await client.GetFromJsonAsync<List<CourseResponse>>("/api/courses", Json))!.Single(c => c.Id == 4);

        var response = await client.PutAsJsonAsync("/api/courses/4",
            new CourseDetailsRequest(new DateOnly(2026, 10, 24), original.Platform, original.Name, original.Area, original.Url, original.EstimatedHours), Json);

        response.EnsureSuccessStatusCode();
        var courses = (await client.GetFromJsonAsync<List<CourseResponse>>("/api/courses", Json))!;
        Assert.Equal(1, courses.Single(c => c.Id == 4).Day);
        Assert.Equal(2, courses.Single(c => c.Id == 1).Day);
    }

    [Fact]
    public async Task Deleting_a_course_removes_it()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/courses/11")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/courses/11")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/courses/11")).StatusCode);
    }

    [Fact]
    public async Task Invalid_course_details_return_validation_problem()
    {
        await using var factory = new ApiFactory();
        var client = await factory.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/courses", Details(name: "", hours: 0), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Informe o nome do curso.", body);
        Assert.Contains("As horas estimadas devem ser maiores que zero.", body);
    }
}
