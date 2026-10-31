namespace CursoTracker.Api.Domain;

public enum LearningGoal
{
    GetJob,
    GrowInCurrentJob,
    ChangeCareer,
    Curiosity,
}

public enum SkillLevel
{
    Beginner,
    Intermediate,
    Advanced,
}

public enum CourseLanguage
{
    Portuguese,
    English,
    Any,
}

public sealed record ProfileAnswers(
    IReadOnlyList<string> Topics,
    LearningGoal Goal,
    SkillLevel Level,
    CourseLanguage Language,
    int HoursPerWeek);

/// <summary>O que a pessoa procura; guia as buscas da IA.</summary>
public sealed class UserProfile
{
    public string UserId { get; private set; } = "";
    public List<string> Topics { get; private set; } = [];
    public LearningGoal Goal { get; private set; }
    public SkillLevel Level { get; private set; }
    public CourseLanguage Language { get; private set; }
    public int HoursPerWeek { get; private set; }
    public DateOnly? SearchDate { get; private set; }
    public int SearchesOnDate { get; private set; }
    /// <summary>Primeiro dia do mês em que os cursos grátis foram liberados.</summary>
    public DateOnly? RevealMonth { get; private set; }
    public int RevealedInMonth { get; private set; }

    private UserProfile() { } // EF Core

    public UserProfile(string userId, ProfileAnswers answers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        Update(answers);
    }

    public void Update(ProfileAnswers answers)
    {
        var topics = answers.Topics
            .Select(t => t?.Trim() ?? "")
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (topics.Count == 0)
            throw new ArgumentException("Informe pelo menos um assunto.", nameof(answers));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(answers.HoursPerWeek);

        Topics = topics;
        Goal = answers.Goal;
        Level = answers.Level;
        Language = answers.Language;
        HoursPerWeek = answers.HoursPerWeek;
    }

    public int RemainingSearches(DateOnly today, int dailyLimit) =>
        SearchDate == today ? Math.Max(0, dailyLimit - SearchesOnDate) : dailyLimit;

    /// <summary>Reserva uma busca do limite diário; devolve false quando o limite acabou.</summary>
    public bool TryUseSearch(DateOnly today, int dailyLimit)
    {
        if (SearchDate != today)
        {
            SearchDate = today;
            SearchesOnDate = 0;
        }
        if (SearchesOnDate >= dailyLimit)
            return false;
        SearchesOnDate++;
        return true;
    }

    /// <summary>Devolve a busca reservada quando ela falhou por erro do serviço.</summary>
    public void RefundSearch(DateOnly today)
    {
        if (SearchDate == today && SearchesOnDate > 0)
            SearchesOnDate--;
    }

    public int FreeCoursesLeft(DateOnly today, int monthlyLimit) =>
        RevealMonth == MonthOf(today) ? Math.Max(0, monthlyLimit - RevealedInMonth) : monthlyLimit;

    /// <summary>Libera até <paramref name="wanted"/> cursos da cota grátis do mês; devolve quantos foram liberados.</summary>
    public int RevealCourses(DateOnly today, int wanted, int monthlyLimit)
    {
        if (RevealMonth != MonthOf(today))
        {
            RevealMonth = MonthOf(today);
            RevealedInMonth = 0;
        }
        var granted = Math.Clamp(wanted, 0, Math.Max(0, monthlyLimit - RevealedInMonth));
        RevealedInMonth += granted;
        return granted;
    }

    static DateOnly MonthOf(DateOnly day) => new(day.Year, day.Month, 1);
}
