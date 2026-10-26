using CursoTracker.Api.Domain;

namespace CursoTracker.Api.Tests;

public class CoursePlanTests
{
    static Course On(string date) =>
        new("user-1", 1, DateOnly.Parse(date), "Plataforma", "Curso", "Cloud", "https://example.com", 2);

    [Fact]
    public void Earliest_date_is_day_one_and_gaps_are_kept()
    {
        var courses = new[] { On("2026-10-27"), On("2026-10-25"), On("2026-10-25"), On("2026-11-02") };

        CoursePlan.Renumber(courses);

        Assert.Equal([3, 1, 1, 9], courses.Select(c => c.Day));
    }

    [Fact]
    public void Moving_a_course_before_the_start_shifts_everyone()
    {
        var first = On("2026-10-25");
        var second = On("2026-10-26");
        CoursePlan.Renumber([first, second]);

        second.UpdateDetails(new CourseDetails(new DateOnly(2026, 10, 24), "Plataforma", "Curso", "Cloud", "https://example.com", 2));
        CoursePlan.Renumber([first, second]);

        Assert.Equal((2, 1), (first.Day, second.Day));
    }
}
