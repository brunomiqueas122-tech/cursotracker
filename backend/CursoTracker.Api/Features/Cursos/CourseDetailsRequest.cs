using CursoTracker.Api.Domain;
using FluentValidation;

namespace CursoTracker.Api.Features.Cursos;

public sealed record CourseDetailsRequest(
    DateOnly Date,
    string Platform,
    string Name,
    string Area,
    string Url,
    double EstimatedHours)
{
    public CourseDetails ToDetails() => new(Date, Platform, Name, Area, Url, EstimatedHours);
}

public sealed class CourseDetailsValidator : AbstractValidator<CourseDetailsRequest>
{
    public CourseDetailsValidator()
    {
        RuleFor(r => r.Date).NotEqual(default(DateOnly)).WithMessage("Informe a data.");
        RuleFor(r => r.Name).NotEmpty().WithMessage("Informe o nome do curso.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(r => r.Platform).NotEmpty().WithMessage("Informe a plataforma.")
            .MaximumLength(100).WithMessage("A plataforma pode ter no máximo 100 caracteres.");
        RuleFor(r => r.Area).NotEmpty().WithMessage("Informe a área.")
            .MaximumLength(100).WithMessage("A área pode ter no máximo 100 caracteres.");
        RuleFor(r => r.Url)
            .NotEmpty().WithMessage("Informe o link do curso.")
            .MaximumLength(500)
            .Must(Urls.IsHttp).WithMessage("Informe um link começando com http:// ou https://.");
        RuleFor(r => r.EstimatedHours)
            .GreaterThan(0).WithMessage("As horas estimadas devem ser maiores que zero.")
            .LessThanOrEqualTo(500).WithMessage("Informe no máximo 500 horas.");
    }
}

static class Urls
{
    public static bool IsHttp(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
