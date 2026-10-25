using CursoTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CursoTracker.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(course =>
        {
            course.Property(c => c.Platform).HasMaxLength(100).IsRequired();
            course.Property(c => c.Name).HasMaxLength(200).IsRequired();
            course.Property(c => c.Area).HasMaxLength(100).IsRequired();
            course.Property(c => c.Url).HasMaxLength(500).IsRequired();
            course.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            course.Property(c => c.CertificateUrl).HasMaxLength(500);
            course.Property(c => c.Notes).HasMaxLength(1000);
            course.HasIndex(c => c.Day);
        });
    }
}
