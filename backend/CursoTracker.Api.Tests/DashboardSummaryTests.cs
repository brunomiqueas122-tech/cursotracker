using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Dashboard;

namespace CursoTracker.Api.Tests;

public class DashboardSummaryTests
{
    static readonly DateTimeOffset Now = new(2026, 10, 26, 20, 0, 0, TimeSpan.Zero);

    static Course Course(int day, double estimated, double done, CourseStatus status, bool cert = false)
    {
        var course = new Course(day, new DateOnly(2026, 10, 24 + day), "Plataforma", $"Curso {day}", "Cloud", "https://example.com", estimated);
        course.UpdateProgress(new CourseProgress(done, status, cert, null, false, null), Now);
        return course;
    }

    [Fact]
    public void Skipped_courses_do_not_count_toward_goal()
    {
        var courses = new[]
        {
            Course(1, 5, 5, CourseStatus.Completed, cert: true),
            Course(1, 3, 1, CourseStatus.InProgress),
            Course(2, 2, 0, CourseStatus.Skipped),
        };

        var summary = DashboardSummary.From(courses);

        Assert.Equal(50, summary.CompletedPercent);
        Assert.Equal(8, summary.EstimatedHours);
        Assert.Equal(6, summary.HoursDone);
        Assert.Equal(1, summary.SkippedCourses);
    }

    [Fact]
    public void Certificates_pending_are_completed_courses_without_certificate()
    {
        var courses = new[]
        {
            Course(1, 5, 5, CourseStatus.Completed, cert: true),
            Course(2, 3, 3, CourseStatus.Completed),
        };

        var summary = DashboardSummary.From(courses);

        Assert.Equal(1, summary.CertificatesReceived);
        Assert.Equal(1, summary.CertificatesPending);
    }

    [Fact]
    public void Groups_hours_by_day_in_order()
    {
        var courses = new[]
        {
            Course(2, 2, 1, CourseStatus.InProgress),
            Course(1, 5, 5, CourseStatus.Completed),
            Course(2, 3, 0, CourseStatus.NotStarted),
        };

        var days = DashboardSummary.From(courses).Days;

        Assert.Equal([1, 2], days.Select(d => d.Day));
        Assert.Equal(new DaySummary(2, new DateOnly(2026, 10, 26), 2, 0, 5, 1), days[1]);
    }

    [Fact]
    public void Empty_list_has_zero_percent()
    {
        Assert.Equal(0, DashboardSummary.From([]).CompletedPercent);
    }
}
