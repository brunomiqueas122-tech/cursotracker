using CursoTracker.Api.Data;
using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Recommendations;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CursoTracker.Api.Features.Profile;

public sealed record ProfileRequest(
    string LookingFor,
    LearningGoal Goal,
    SkillLevel Level,
    CourseLanguage Language,
    int HoursPerWeek)
{
    public ProfileAnswers ToAnswers() => new(LookingFor, Goal, Level, Language, HoursPerWeek);
}

public sealed record ProfileResponse(
    string LookingFor,
    LearningGoal Goal,
    SkillLevel Level,
    CourseLanguage Language,
    int HoursPerWeek,
    int RemainingSearchesToday);

public sealed class ProfileValidator : AbstractValidator<ProfileRequest>
{
    public ProfileValidator()
    {
        RuleFor(r => r.LookingFor).NotEmpty().WithMessage("Conte que tipo de curso você está procurando.")
            .MaximumLength(500).WithMessage("Use no máximo 500 caracteres.");
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
        HttpContext http, AppDbContext db, TimeProvider time, IOptions<AiOptions> ai, CancellationToken ct)
    {
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == http.User.UserId(), ct);
        return profile is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(profile, time, ai.Value));
    }

    static async Task<Results<Ok<ProfileResponse>, ValidationProblem>> SaveProfile(
        ProfileRequest request,
        IValidator<ProfileRequest> validator,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        IOptions<AiOptions> ai,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var userId = http.User.UserId();
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
            db.Profiles.Add(profile = new UserProfile(userId, request.ToAnswers()));
        else
            profile.Update(request.ToAnswers());

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToResponse(profile, time, ai.Value));
    }

    static ProfileResponse ToResponse(UserProfile p, TimeProvider time, AiOptions ai) =>
        new(p.LookingFor, p.Goal, p.Level, p.Language, p.HoursPerWeek,
            p.RemainingSearches(DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime), ai.DailySearchLimit));
}
