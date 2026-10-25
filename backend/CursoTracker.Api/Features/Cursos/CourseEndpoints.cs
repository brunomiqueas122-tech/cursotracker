using CursoTracker.Api.Data;
using CursoTracker.Api.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CursoTracker.Api.Features.Cursos;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourses(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses");
        group.MapGet("/", ListCourses);
        group.MapGet("/{id:int}", GetCourse);
        group.MapPut("/{id:int}/progress", UpdateProgress);
        return app;
    }

    static async Task<Ok<List<CourseResponse>>> ListCourses(
        AppDbContext db, CourseStatus? status, string? area, CancellationToken ct)
    {
        var query = db.Courses.AsNoTracking();
        if (status is not null)
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(area))
            query = query.Where(c => c.Area == area);

        var courses = await query.OrderBy(c => c.Day).ThenBy(c => c.Id).ToListAsync(ct);
        return TypedResults.Ok(courses.Select(CourseResponse.From).ToList());
    }

    static async Task<Results<Ok<CourseResponse>, NotFound>> GetCourse(int id, AppDbContext db, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return course is null ? TypedResults.NotFound() : TypedResults.Ok(CourseResponse.From(course));
    }

    static async Task<Results<Ok<CourseResponse>, NotFound, ValidationProblem>> UpdateProgress(
        int id,
        UpdateProgressRequest request,
        IValidator<UpdateProgressRequest> validator,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
            return TypedResults.NotFound();

        course.UpdateProgress(request.ToProgress(), time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(CourseResponse.From(course));
    }
}
