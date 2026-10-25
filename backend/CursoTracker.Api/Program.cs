using System.Text.Json.Serialization;
using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Cursos;
using CursoTracker.Api.Features.Dashboard;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=cursotracker.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IValidator<UpdateProgressRequest>, UpdateProgressValidator>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seedFile = app.Configuration["SeedFile"] ?? Path.Combine(AppContext.BaseDirectory, "seed-courses.json");
    await SeedData.EnsureSeededAsync(
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        seedFile,
        scope.ServiceProvider.GetRequiredService<TimeProvider>(),
        app.Lifetime.ApplicationStopping);
}

app.MapCourses();
app.MapDashboard();

app.Run();

public partial class Program;
