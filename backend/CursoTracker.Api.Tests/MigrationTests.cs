using CursoTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace CursoTracker.Api.Tests;

[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgresFixture db)
{
    [Fact]
    public async Task Old_profile_text_becomes_the_first_topic()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.NewDatabase()).Options;
        await using var context = new AppDbContext(options);
        var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync("AddRecommendationRuns");
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "AspNetUsers" ("Id", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
            VALUES ('u1', false, false, false, false, 0);
            INSERT INTO "Profiles" ("UserId", "LookingFor", "Goal", "Level", "Language", "HoursPerWeek", "SearchesOnDate")
            VALUES ('u1', 'Quero aprender C#', 'GetJob', 'Beginner', 'Any', 10, 0);
            """);

        await migrator.MigrateAsync();

        var profile = await context.Profiles.SingleAsync();
        Assert.Equal(["Quero aprender C#"], profile.Topics);
        Assert.Equal(0, profile.RevealedInMonth);
        Assert.Null(await context.Users.Select(u => u.PremiumUntil).SingleAsync());
    }
}
