using CursoTracker.Api.Domain;
using FluentValidation;

namespace CursoTracker.Api.Features.Cursos;

/// <summary>Arquivo de backup do app antigo (modo celular): <c>{ "courses": [...] }</c>; campos extras são ignorados.</summary>
public sealed record ImportRequest(List<ImportedCourse> Courses);

public sealed record ImportedCourse(
    DateOnly Date,
    string Platform,
    string Name,
    string Area,
    string Url,
    double EstimatedHours,
    double HoursDone,
    CourseStatus Status,
    DateTimeOffset? CompletedAt,
    bool CertificateReceived,
    string? CertificateUrl,
    bool OnLinkedIn,
    string? Notes)
{
    public CourseDetailsRequest ToDetails() => new(Date, Platform, Name, Area, Url, EstimatedHours);
    public UpdateProgressRequest ToProgressRequest() => new(HoursDone, Status, CertificateReceived, CertificateUrl, OnLinkedIn, Notes);
    public CourseProgress ToProgress() => ToProgressRequest().ToProgress();
}

public sealed record ImportResponse(int Imported);

public sealed class ImportValidator : AbstractValidator<ImportRequest>
{
    public ImportValidator()
    {
        RuleFor(r => r.Courses)
            .NotEmpty().WithMessage("O arquivo não tem cursos.")
            .Must(c => c.Count <= 500).WithMessage("Importe no máximo 500 cursos por vez.");
        RuleForEach(r => r.Courses).ChildRules(course =>
        {
            course.RuleFor(c => c.ToDetails()).SetValidator(new CourseDetailsValidator()).OverridePropertyName("");
            course.RuleFor(c => c.ToProgressRequest()).SetValidator(new UpdateProgressValidator()).OverridePropertyName("");
        });
    }
}
