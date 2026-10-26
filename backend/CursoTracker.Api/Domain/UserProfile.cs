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
    string LookingFor,
    LearningGoal Goal,
    SkillLevel Level,
    CourseLanguage Language,
    int HoursPerWeek);

/// <summary>O que a pessoa procura; guia as buscas da IA.</summary>
public sealed class UserProfile
{
    public string UserId { get; private set; } = "";
    public string LookingFor { get; private set; } = "";
    public LearningGoal Goal { get; private set; }
    public SkillLevel Level { get; private set; }
    public CourseLanguage Language { get; private set; }
    public int HoursPerWeek { get; private set; }
    public DateOnly? SearchDate { get; private set; }
    public int SearchesOnDate { get; private set; }

    private UserProfile() { } // EF Core

    public UserProfile(string userId, ProfileAnswers answers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        Update(answers);
    }

    public void Update(ProfileAnswers answers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(answers.LookingFor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(answers.HoursPerWeek);

        LookingFor = answers.LookingFor.Trim();
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
}
