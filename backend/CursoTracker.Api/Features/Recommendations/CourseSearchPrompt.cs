using System.Text;
using CursoTracker.Api.Domain;

namespace CursoTracker.Api.Features.Recommendations;

/// <summary>Regras e pedido da busca, iguais para qualquer provedor de IA.</summary>
public static class CourseSearchPrompt
{
    public const string Rules = """
        Você ajuda brasileiros a encontrar cursos online gratuitos que dão certificado gratuito.

        Use a busca na web para encontrar opções atuais. Só inclua um curso depois de confirmar, em uma página
        oficial do provedor, que o curso e o certificado são gratuitos. Descarte cursos em que só o conteúdo é
        grátis e o certificado é pago (como o modo "auditar" de algumas plataformas), cursos com teste grátis
        que depois cobram, e páginas de revenda ou agregadores duvidosos.

        Escolha os cursos pelo perfil da pessoa: o que ela procura, objetivo, nível, idioma preferido e tempo
        disponível. Prefira provedores reconhecidos pelo mercado. Não sugira cursos cujo link já está no plano dela.
        Escreva os textos em português do Brasil.
        """;

    public static string DescribeRequest(UserProfile profile, string? refinement, IReadOnlyCollection<string> knownUrls, DateTimeOffset now)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Data de hoje: {now:yyyy-MM-dd}");
        sb.AppendLine($"O que procuro: {profile.LookingFor}");
        sb.AppendLine($"Objetivo: {Describe(profile.Goal)}");
        sb.AppendLine($"Nível: {Describe(profile.Level)}");
        sb.AppendLine($"Idioma dos cursos: {Describe(profile.Language)}");
        sb.AppendLine($"Tempo disponível: {profile.HoursPerWeek} horas por semana");
        if (!string.IsNullOrWhiteSpace(refinement))
            sb.AppendLine($"Ajuste pedido nesta busca: {refinement}");
        if (knownUrls.Count > 0)
        {
            sb.AppendLine("Cursos que já estão no meu plano (não repita):");
            foreach (var url in knownUrls.Take(100))
                sb.AppendLine($"- {url}");
        }
        return sb.ToString();
    }

    static string Describe(LearningGoal goal) => goal switch
    {
        LearningGoal.GetJob => "conseguir um emprego",
        LearningGoal.GrowInCurrentJob => "crescer no trabalho atual",
        LearningGoal.ChangeCareer => "mudar de área",
        _ => "aprender por interesse pessoal",
    };

    static string Describe(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => "iniciante",
        SkillLevel.Intermediate => "intermediário",
        _ => "avançado",
    };

    static string Describe(CourseLanguage language) => language switch
    {
        CourseLanguage.Portuguese => "português",
        CourseLanguage.English => "inglês",
        _ => "português ou inglês",
    };
}
