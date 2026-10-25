using System.Text.Json;
using System.Text.Json.Serialization;
using CursoTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CursoTracker.Api.Data;

public static class SeedData
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task EnsureSeededAsync(AppDbContext db, string seedFile, TimeProvider time, CancellationToken ct)
    {
        // Schema criado direto do modelo: app pessoal, sem histórico de migrações a preservar ainda
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Courses.AnyAsync(ct))
            return;

        await using var stream = File.OpenRead(seedFile);
        var seeds = await JsonSerializer.DeserializeAsync<List<SeedCourse>>(stream, JsonOptions, ct) ?? [];

        foreach (var seed in seeds)
        {
            var course = new Course(seed.Day, seed.Date, seed.Platform, seed.Name, seed.Area, seed.Url, seed.EstimatedHours);
            course.UpdateProgress(
                new CourseProgress(seed.HoursDone, seed.Status, seed.CertificateReceived, seed.CertificateUrl, seed.OnLinkedIn, seed.Notes),
                seed.CompletedAt ?? time.GetUtcNow());
            db.Courses.Add(course);
        }
        await db.SaveChangesAsync(ct);
    }

    sealed record SeedCourse(
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
        string? Notes);
}
