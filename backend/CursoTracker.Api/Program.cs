using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Anthropic;
using CursoTracker.Api.Data;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Cursos;
using CursoTracker.Api.Features.Dashboard;
using CursoTracker.Api.Features.Profile;
using CursoTracker.Api.Features.Recommendations;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = ConnectionStrings.ToNpgsql(builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Defina a conexão do PostgreSQL em ConnectionStrings:Default."));
if (string.IsNullOrEmpty(builder.Configuration["Auth:InviteCode"]))
    throw new InvalidOperationException("Defina o código de convite do cadastro em Auth:InviteCode (variável Auth__InviteCode).");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<PortugueseIdentityErrorDescriber>();
builder.Services.ConfigureApplicationCookie(options =>
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
// O plano grátis do Render reinicia o servidor; com as chaves no banco o login continua valendo
builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>().SetApplicationName("CursoTracker");
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthEndpoints.RateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
// Sem chave configurada o app sobe normalmente; só a busca com IA responde erro
builder.Services.AddSingleton(_ => new AnthropicClient { ApiKey = builder.Configuration["ANTHROPIC_API_KEY"] ?? "" });
builder.Services.AddSingleton<ICourseFinder, ClaudeCourseFinder>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(app.Lifetime.ApplicationStopping);

app.MapGet("/api/health", () => TypedResults.Ok("ok"));
app.MapAuth();
var api = app.MapGroup("").RequireAuthorization();
api.MapCourses();
api.MapDashboard();
api.MapProfile();
api.MapRecommendations();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
