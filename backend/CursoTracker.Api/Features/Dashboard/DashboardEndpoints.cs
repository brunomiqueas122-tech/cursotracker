using CursoTracker.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CursoTracker.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", GetDashboard);
        return app;
    }

    static async Task<Ok<DashboardSummary>> GetDashboard(AppDbContext db, CancellationToken ct)
    {
        var courses = await db.Courses.AsNoTracking().ToListAsync(ct);
        return TypedResults.Ok(DashboardSummary.From(courses));
    }
}
