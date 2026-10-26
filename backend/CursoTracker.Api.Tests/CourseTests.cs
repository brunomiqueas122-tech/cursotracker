using CursoTracker.Api.Domain;

namespace CursoTracker.Api.Tests;

public class CourseTests
{
    static readonly DateTimeOffset Now = new(2026, 10, 25, 20, 0, 0, TimeSpan.Zero);

    static Course NewCourse() =>
        new("user-1", 1, new DateOnly(2026, 10, 25), "Linux Foundation", "LFD121", "Segurança", "https://example.com", 5);

    static CourseProgress Progress(double hours, CourseStatus status, bool cert = false, string? certUrl = null) =>
        new(hours, status, cert, certUrl, OnLinkedIn: false, Notes: null);

    [Fact]
    public void Completing_sets_completed_at_once()
    {
        var course = NewCourse();

        course.UpdateProgress(Progress(5, CourseStatus.Completed), Now);
        course.UpdateProgress(Progress(5, CourseStatus.Completed), Now.AddDays(1));

        Assert.Equal(Now, course.CompletedAt);
    }

    [Fact]
    public void Leaving_completed_clears_completed_at()
    {
        var course = NewCourse();
        course.UpdateProgress(Progress(5, CourseStatus.Completed), Now);

        course.UpdateProgress(Progress(4, CourseStatus.InProgress), Now);

        Assert.Null(course.CompletedAt);
    }

    [Fact]
    public void Logging_hours_on_not_started_course_moves_it_to_in_progress()
    {
        var course = NewCourse();

        course.UpdateProgress(Progress(1.5, CourseStatus.NotStarted), Now);

        Assert.Equal(CourseStatus.InProgress, course.Status);
    }

    [Fact]
    public void Certificate_url_is_dropped_when_certificate_not_received()
    {
        var course = NewCourse();

        course.UpdateProgress(Progress(5, CourseStatus.Completed, cert: false, certUrl: "https://cert"), Now);

        Assert.Null(course.CertificateUrl);
    }

    [Fact]
    public void Negative_hours_are_rejected()
    {
        var course = NewCourse();

        Assert.Throws<ArgumentOutOfRangeException>(() => course.UpdateProgress(Progress(-1, CourseStatus.InProgress), Now));
    }
}
