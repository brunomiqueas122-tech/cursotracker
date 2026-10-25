using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Cursos;
using CursoTracker.Api.Features.Dashboard;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrEmpty(builder.Configuration["Auth:Password"]))
    throw new InvalidOperationException("Defina a senha de acesso em Auth:Password (variável Auth__Password).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=cursotracker.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IValidator<UpdateProgressRequest>, UpdateProgressValidator>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "cursotracker";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        // API: responde 401/403 em vez de redirecionar para uma página de login
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(AuthEndpoints.LoginRateLimit, limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seedFile = app.Configuration["SeedFile"] ?? Path.Combine(AppContext.BaseDirectory, "seed-courses.json");
    await SeedData.EnsureSeededAsync(
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        seedFile,
        scope.ServiceProvider.GetRequiredService<TimeProvider>(),
        app.Lifetime.ApplicationStopping);
}

app.MapAuth();
var api = app.MapGroup("").RequireAuthorization();
api.MapCourses();
api.MapDashboard();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
