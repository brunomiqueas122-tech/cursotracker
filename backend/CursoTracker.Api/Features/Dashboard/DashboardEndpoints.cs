using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
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

    static async Task<Ok<DashboardSummary>> GetDashboard(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var userId = http.User.UserId();
        var courses = await db.Courses.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(ct);
        return TypedResults.Ok(DashboardSummary.From(courses));
    }
}
