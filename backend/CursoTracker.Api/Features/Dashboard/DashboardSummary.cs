using CursoTracker.Api.Domain;

namespace CursoTracker.Api.Features.Dashboard;

public sealed record DaySummary(
    int Day,
    DateOnly Date,
    int Courses,
    int Completed,
    double EstimatedHours,
    double HoursDone);

public sealed record DashboardSummary(
    int TotalCourses,
    int CompletedCourses,
    int SkippedCourses,
    double CompletedPercent,
    int CertificatesReceived,
    int CertificatesPending,
    int OnLinkedIn,
    double EstimatedHours,
    double HoursDone,
    IReadOnlyList<DaySummary> Days)
{
    // Cursos pulados saem da meta: não contam no % nem nas horas estimadas
    public static DashboardSummary From(IReadOnlyCollection<Course> courses)
    {
        var planned = courses.Where(c => c.Status != CourseStatus.Skipped).ToList();
        var completed = planned.Count(c => c.Status == CourseStatus.Completed);

        var days = courses
            .GroupBy(c => c.Day)
            .OrderBy(g => g.Key)
            .Select(g => new DaySummary(
                g.Key,
                g.Min(c => c.Date),
                g.Count(c => c.Status != CourseStatus.Skipped),
                g.Count(c => c.Status == CourseStatus.Completed),
                g.Where(c => c.Status != CourseStatus.Skipped).Sum(c => c.EstimatedHours),
                g.Sum(c => c.HoursDone)))
            .ToList();

        return new DashboardSummary(
            TotalCourses: courses.Count,
            CompletedCourses: completed,
            SkippedCourses: courses.Count - planned.Count,
            CompletedPercent: planned.Count == 0 ? 0 : Math.Round(100.0 * completed / planned.Count, 1),
            CertificatesReceived: courses.Count(c => c.CertificateReceived),
            CertificatesPending: courses.Count(c => c.Status == CourseStatus.Completed && !c.CertificateReceived),
            OnLinkedIn: courses.Count(c => c.OnLinkedIn),
            EstimatedHours: planned.Sum(c => c.EstimatedHours),
            HoursDone: courses.Sum(c => c.HoursDone),
            Days: days);
    }
}
