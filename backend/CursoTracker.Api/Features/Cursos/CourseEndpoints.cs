using CursoTracker.Api.Data;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Auth;
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
        group.MapPost("/", CreateCourse);
        group.MapPost("/import", ImportCourses);
        group.MapPut("/{id:int}", UpdateCourse);
        group.MapDelete("/{id:int}", DeleteCourse);
        group.MapPut("/{id:int}/progress", UpdateProgress);
        return app;
    }

    static IQueryable<Course> Mine(this AppDbContext db, HttpContext http)
    {
        var userId = http.User.UserId();
        return db.Courses.Where(c => c.UserId == userId);
    }

    static async Task<Ok<List<CourseResponse>>> ListCourses(
        HttpContext http, AppDbContext db, CourseStatus? status, string? area, CancellationToken ct)
    {
        var query = db.Mine(http).AsNoTracking();
        if (status is not null)
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(area))
            query = query.Where(c => c.Area == area);

        var courses = await query.OrderBy(c => c.Day).ThenBy(c => c.Id).ToListAsync(ct);
        return TypedResults.Ok(courses.Select(CourseResponse.From).ToList());
    }

    static async Task<Results<Ok<CourseResponse>, NotFound>> GetCourse(int id, HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var course = await db.Mine(http).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return course is null ? TypedResults.NotFound() : TypedResults.Ok(CourseResponse.From(course));
    }

    static async Task<Results<Ok<CourseResponse>, NotFound, ValidationProblem>> UpdateProgress(
        int id,
        UpdateProgressRequest request,
        IValidator<UpdateProgressRequest> validator,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var course = await db.Mine(http).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
            return TypedResults.NotFound();

        course.UpdateProgress(request.ToProgress(), time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(CourseResponse.From(course));
    }

    static async Task<Results<Created<CourseResponse>, ValidationProblem>> CreateCourse(
        CourseDetailsRequest request, IValidator<CourseDetailsRequest> validator, HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var d = request.ToDetails();
        var course = new Course(http.User.UserId(), 1, d.Date, d.Platform, d.Name, d.Area, d.Url, d.EstimatedHours);
        db.Courses.Add(course);
        CoursePlan.Renumber([.. await db.Mine(http).ToListAsync(ct), course]);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/courses/{course.Id}", CourseResponse.From(course));
    }

    static async Task<Results<Ok<ImportResponse>, ValidationProblem>> ImportCourses(
        ImportRequest request,
        IValidator<ImportRequest> validator,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var userId = http.User.UserId();
        var imported = request.Courses.Select(i =>
        {
            var course = new Course(userId, 1, i.Date, i.Platform, i.Name, i.Area, i.Url, i.EstimatedHours);
            course.UpdateProgress(i.ToProgress(), i.CompletedAt ?? time.GetUtcNow());
            return course;
        }).ToList();

        db.Courses.AddRange(imported);
        CoursePlan.Renumber([.. await db.Mine(http).ToListAsync(ct), .. imported]);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new ImportResponse(imported.Count));
    }

    static async Task<Results<Ok<CourseResponse>, NotFound, ValidationProblem>> UpdateCourse(
        int id, CourseDetailsRequest request, IValidator<CourseDetailsRequest> validator, HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var course = await db.Mine(http).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
            return TypedResults.NotFound();

        course.UpdateDetails(request.ToDetails());
        CoursePlan.Renumber(await db.Mine(http).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(CourseResponse.From(course));
    }

    static async Task<Results<NoContent, NotFound>> DeleteCourse(int id, HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var course = await db.Mine(http).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
            return TypedResults.NotFound();

        db.Courses.Remove(course);
        CoursePlan.Renumber(await db.Mine(http).Where(c => c.Id != id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
