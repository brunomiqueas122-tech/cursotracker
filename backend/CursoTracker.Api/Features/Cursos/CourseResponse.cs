using CursoTracker.Api.Domain;

namespace CursoTracker.Api.Features.Cursos;

public sealed record CourseResponse(
    int Id,
    int Day,
    DateOnly Date,
    string Platform,
    string Name,
    string Area,
    string Url,
    double EstimatedHours,
    double HoursDone,
    CourseStatus Status,
    DateTimeOffset? CompletedAt,
    bool CertificateReceived,
    string? CertificateUrl,
    bool OnLinkedIn,
    string? Notes)
{
    public static CourseResponse From(Course c) => new(
        c.Id, c.Day, c.Date, c.Platform, c.Name, c.Area, c.Url, c.EstimatedHours, c.HoursDone,
        c.Status, c.CompletedAt, c.CertificateReceived, c.CertificateUrl, c.OnLinkedIn, c.Notes);
}
