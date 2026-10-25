namespace CursoTracker.Api.Domain;

public enum CourseStatus
{
    NotStarted,
    InProgress,
    Completed,
    Skipped,
}

public sealed record CourseProgress(
    double HoursDone,
    CourseStatus Status,
    bool CertificateReceived,
    string? CertificateUrl,
    bool OnLinkedIn,
    string? Notes);

public sealed record CourseDetails(
    DateOnly Date,
    string Platform,
    string Name,
    string Area,
    string Url,
    double EstimatedHours);

public sealed class Course
{
    public int Id { get; private set; }
    public int Day { get; private set; }
    public DateOnly Date { get; private set; }
    public string Platform { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Area { get; private set; } = "";
    public string Url { get; private set; } = "";
    public double EstimatedHours { get; private set; }
    public double HoursDone { get; private set; }
    public CourseStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool CertificateReceived { get; private set; }
    public string? CertificateUrl { get; private set; }
    public bool OnLinkedIn { get; private set; }
    public string? Notes { get; private set; }

    private Course() { } // EF Core

    public Course(int day, DateOnly date, string platform, string name, string area, string url, double estimatedHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(day);

        Day = day;
        Status = CourseStatus.NotStarted;
        UpdateDetails(new CourseDetails(date, platform, name, area, url, estimatedHours));
    }

    public void UpdateDetails(CourseDetails details)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(details.EstimatedHours);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Name);

        Date = details.Date;
        Platform = details.Platform.Trim();
        Name = details.Name.Trim();
        Area = details.Area.Trim();
        Url = details.Url.Trim();
        EstimatedHours = details.EstimatedHours;
    }

    internal void AssignDay(int day)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(day);
        Day = day;
    }

    public void UpdateProgress(CourseProgress progress, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(progress.HoursDone);

        var status = progress.Status;
        // Registrar horas num curso não iniciado significa que ele começou
        if (status == CourseStatus.NotStarted && progress.HoursDone > 0)
            status = CourseStatus.InProgress;

        if (status == CourseStatus.Completed)
            CompletedAt ??= now;
        else
            CompletedAt = null;

        Status = status;
        HoursDone = progress.HoursDone;
        CertificateReceived = progress.CertificateReceived;
        CertificateUrl = progress.CertificateReceived ? Normalize(progress.CertificateUrl) : null;
        OnLinkedIn = progress.OnLinkedIn;
        Notes = Normalize(progress.Notes);
    }

    static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
