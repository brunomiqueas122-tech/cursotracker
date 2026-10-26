using CursoTracker.Api.Domain;

namespace CursoTracker.Api.Tests;

public class UserProfileTests
{
    static readonly DateOnly Today = new(2026, 10, 26);

    static UserProfile NewProfile() =>
        new("user-1", new ProfileAnswers("Cloud", LearningGoal.GetJob, SkillLevel.Beginner, CourseLanguage.Portuguese, 5));

    [Fact]
    public void Daily_search_limit_resets_on_the_next_day()
    {
        var profile = NewProfile();

        Assert.True(profile.TryUseSearch(Today, 2));
        Assert.True(profile.TryUseSearch(Today, 2));
        Assert.False(profile.TryUseSearch(Today, 2));
        Assert.Equal(0, profile.RemainingSearches(Today, 2));

        Assert.Equal(2, profile.RemainingSearches(Today.AddDays(1), 2));
        Assert.True(profile.TryUseSearch(Today.AddDays(1), 2));
    }

    [Fact]
    public void Failed_search_is_given_back()
    {
        var profile = NewProfile();
        profile.TryUseSearch(Today, 2);

        profile.RefundSearch(Today);

        Assert.Equal(2, profile.RemainingSearches(Today, 2));
    }

    [Fact]
    public void Answers_are_trimmed_and_validated()
    {
        var profile = NewProfile();
        profile.Update(new ProfileAnswers("  Dados  ", LearningGoal.ChangeCareer, SkillLevel.Intermediate, CourseLanguage.Any, 10));

        Assert.Equal("Dados", profile.LookingFor);
        Assert.Throws<ArgumentException>(() => profile.Update(new ProfileAnswers(" ", LearningGoal.GetJob, SkillLevel.Beginner, CourseLanguage.Any, 5)));
    }
}
