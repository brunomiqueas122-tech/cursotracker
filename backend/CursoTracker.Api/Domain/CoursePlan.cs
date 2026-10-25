namespace CursoTracker.Api.Domain;

public static class CoursePlan
{
    /// <summary>O dia 1 é a data mais antiga do plano; cada curso fica no dia correspondente à sua data.</summary>
    public static void Renumber(IReadOnlyCollection<Course> courses)
    {
        if (courses.Count == 0)
            return;

        var start = courses.Min(c => c.Date).DayNumber;
        foreach (var course in courses)
            course.AssignDay(course.Date.DayNumber - start + 1);
    }
}
