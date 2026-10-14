namespace GitWerka.Infrastructure.Data;

using System;
using System.IO;
using GitWerka.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core database context for GitWerka application persistence.
/// </summary>
public class GitWerkaDbContext : DbContext
{
    public DbSet<RecentRepositoryEntity> RecentRepositories => Set<RecentRepositoryEntity>();

    public DbSet<GitAuthorEntity> Authors => Set<GitAuthorEntity>();

    public DbSet<GitProfileCredentialsEntity> ProfileCredentials => Set<GitProfileCredentialsEntity>();

    public DbSet<GitProfileEntity> Profiles => Set<GitProfileEntity>();

    public DbSet<RepositoryProfileMappingEntity> RepositoryProfiles => Set<RepositoryProfileMappingEntity>();

    public DbSet<AiConfigurationEntity> AiConfigurations => Set<AiConfigurationEntity>();

    public DbSet<RepositoryAiContextEntity> RepositoryAiContexts => Set<RepositoryAiContextEntity>();

    public DbSet<ApplicationSettingsEntity> ApplicationSettings => Set<ApplicationSettingsEntity>();

    public GitWerkaDbContext()
    {
    }

    public GitWerkaDbContext(DbContextOptions<GitWerkaDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GitWerka");

            Directory.CreateDirectory(appDataPath);

            string databasePath = Path.Combine(appDataPath, "gitwerka.db");
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RecentRepositoryEntity>()
            .HasIndex(r => r.Path)
            .IsUnique();

        modelBuilder.Entity<RepositoryProfileMappingEntity>()
            .HasIndex(m => m.RepositoryPath)
            .IsUnique();

        modelBuilder.Entity<GitProfileEntity>()
            .HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GitProfileEntity>()
            .HasOne(p => p.Credentials)
            .WithMany()
            .HasForeignKey(p => p.CredentialsId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RepositoryProfileMappingEntity>()
            .HasOne(m => m.Profile)
            .WithMany()
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RepositoryAiContextEntity>()
            .HasIndex(c => c.RepositoryPath)
            .IsUnique();
    }
}
