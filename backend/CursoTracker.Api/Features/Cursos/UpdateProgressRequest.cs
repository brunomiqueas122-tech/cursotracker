using CursoTracker.Api.Domain;
using FluentValidation;

namespace CursoTracker.Api.Features.Cursos;

public sealed record UpdateProgressRequest(
    double HoursDone,
    CourseStatus Status,
    bool CertificateReceived,
    string? CertificateUrl,
    bool OnLinkedIn,
    string? Notes)
{
    public CourseProgress ToProgress() =>
        new(HoursDone, Status, CertificateReceived, CertificateUrl, OnLinkedIn, Notes);
}

public sealed class UpdateProgressValidator : AbstractValidator<UpdateProgressRequest>
{
    public UpdateProgressValidator()
    {
        RuleFor(r => r.HoursDone)
            .GreaterThanOrEqualTo(0).WithMessage("As horas feitas não podem ser negativas.")
            .LessThanOrEqualTo(500).WithMessage("Informe no máximo 500 horas.");

        RuleFor(r => r.Status).IsInEnum().WithMessage("Status inválido.");

        RuleFor(r => r.CertificateUrl)
            .MaximumLength(500)
            .Must(BeHttpUrl).WithMessage("Informe um link começando com http:// ou https://.")
            .When(r => !string.IsNullOrWhiteSpace(r.CertificateUrl));

        RuleFor(r => r.Notes).MaximumLength(1000).WithMessage("As anotações podem ter no máximo 1000 caracteres.");
    }

    static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
