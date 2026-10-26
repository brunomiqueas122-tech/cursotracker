using CursoTracker.Api.Domain;
using CursoTracker.Api.Features.Auth;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CursoTracker.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();

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
    }
}
