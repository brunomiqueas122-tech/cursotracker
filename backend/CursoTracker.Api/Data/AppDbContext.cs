using CursoTracker.Api.Domain;
using System.Text.Json;
using CursoTracker.Api.Features.Auth;
using CursoTracker.Api.Features.Recommendations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CursoTracker.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<RecommendationRun> RecommendationRuns => Set<RecommendationRun>();

    // Chaves do cookie de login: no banco, para sobreviver aos reinícios do servidor
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Course>(course =>
        {
            course.Property(c => c.Platform).HasMaxLength(100).IsRequired();
            course.Property(c => c.Name).HasMaxLength(200).IsRequired();
            course.Property(c => c.Area).HasMaxLength(100).IsRequired();
            course.Property(c => c.Url).HasMaxLength(500).IsRequired();
            course.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            course.Property(c => c.CertificateUrl).HasMaxLength(500);
            course.Property(c => c.Notes).HasMaxLength(1000);
            course.HasIndex(c => new { c.UserId, c.Day });
            course.HasOne<AppUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(profile =>
        {
            profile.HasKey(p => p.UserId);
            profile.Property(p => p.LookingFor).HasMaxLength(500).IsRequired();
            profile.Property(p => p.Goal).HasConversion<string>().HasMaxLength(30);
            profile.Property(p => p.Level).HasConversion<string>().HasMaxLength(20);
            profile.Property(p => p.Language).HasConversion<string>().HasMaxLength(20);
            profile.HasOne<AppUser>().WithOne().HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecommendationRun>(run =>
        {
            run.Property(r => r.Refinement).HasMaxLength(300);
            run.Property(r => r.Note).HasMaxLength(2000);
            // Sugestões ficam juntas em uma coluna jsonb: são lidas e gravadas sempre inteiras
            run.Property(r => r.Courses)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Web),
                    v => JsonSerializer.Deserialize<List<CourseSuggestion>>(v, JsonSerializerOptions.Web) ?? new(),
                    new ValueComparer<List<CourseSuggestion>>(
                        (a, b) => a!.SequenceEqual(b!),
                        v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                        v => v.ToList()));
            run.HasIndex(r => new { r.UserId, r.CreatedAt });
            run.HasOne<AppUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
