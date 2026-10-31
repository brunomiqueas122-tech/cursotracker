using CursoTracker.Api.Data;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Premium;
using CursoTracker.Api.Features.Recommendations;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Profile;

public sealed record ProfileRequest(
    IReadOnlyList<string> Topics,
    LearningGoal Goal,
    SkillLevel Level,
    CourseLanguage Language,
    int HoursPerWeek)
{
    public ProfileAnswers ToAnswers() => new(Topics, Goal, Level, Language, HoursPerWeek);
}

/// <param name="FreeCoursesLeft">Cursos grátis que ainda podem ser liberados neste mês; null = Premium (sem limite).</param>
/// <param name="TopicLimit">Máximo de assuntos; null = Premium (sem limite).</param>
public sealed record ProfileResponse(
    IReadOnlyList<string> Topics,
    LearningGoal Goal,
    SkillLevel Level,
    CourseLanguage Language,
    int HoursPerWeek,
    int RemainingSearchesToday,
    int? FreeCoursesLeft,
    int? TopicLimit);

public sealed class ProfileValidator : AbstractValidator<ProfileRequest>
{
    public const int MaxTopics = 20;

    public ProfileValidator()
    {
        RuleFor(r => r.Topics)
            .Must(t => t is not null && t.Any(x => !string.IsNullOrWhiteSpace(x))).WithMessage("Conte que tipo de curso você está procurando.")
            .Must(t => t is null || t.Count <= MaxTopics).WithMessage($"Use no máximo {MaxTopics} assuntos.");
        RuleForEach(r => r.Topics).MaximumLength(200).WithMessage("Use no máximo 200 caracteres por assunto.");
        RuleFor(r => r.Goal).IsInEnum().WithMessage("Objetivo inválido.");
        RuleFor(r => r.Level).IsInEnum().WithMessage("Nível inválido.");
        RuleFor(r => r.Language).IsInEnum().WithMessage("Idioma inválido.");
        RuleFor(r => r.HoursPerWeek).InclusiveBetween(1, 80).WithMessage("Informe entre 1 e 80 horas por semana.");
    }
}

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfile(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile");
        group.MapGet("/", GetProfile);
        group.MapPut("/", SaveProfile);
        return app;
    }

    static async Task<Results<Ok<ProfileResponse>, NotFound>> GetProfile(
        HttpContext http, AppDbContext db, TimeProvider time, IOptions<AiOptions> ai, IOptions<PremiumOptions> premium, CancellationToken ct)
    {
        var userId = http.User.UserId();
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(ToResponse(profile, await db.IsPremiumAsync(userId, time, ct), time, ai.Value, premium.Value));
    }

    static async Task<Results<Ok<ProfileResponse>, ValidationProblem, ProblemHttpResult>> SaveProfile(
        ProfileRequest request,
        IValidator<ProfileRequest> validator,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        IOptions<AiOptions> ai,
        IOptions<PremiumOptions> premium,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var userId = http.User.UserId();
        var isPremium = await db.IsPremiumAsync(userId, time, ct);
        var answers = request.ToAnswers();
        var limit = premium.Value.FreeTopics;
        if (!isPremium && answers.Topics.Count(t => !string.IsNullOrWhiteSpace(t)) > limit)
            return PremiumAccess.Required($"No plano grátis você pode ter até {limit} assuntos. Com o Premium eles são ilimitados.");

        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
            db.Profiles.Add(profile = new UserProfile(userId, answers));
        else
            profile.Update(answers);

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToResponse(profile, isPremium, time, ai.Value, premium.Value));
    }

    static ProfileResponse ToResponse(UserProfile p, bool isPremium, TimeProvider time, AiOptions ai, PremiumOptions premium)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return new(p.Topics, p.Goal, p.Level, p.Language, p.HoursPerWeek,
            p.RemainingSearches(today, ai.DailySearchLimit),
            isPremium ? null : p.FreeCoursesLeft(today, premium.FreeCoursesPerMonth),
            isPremium ? null : premium.FreeTopics);
    }
}
