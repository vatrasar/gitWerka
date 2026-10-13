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
    }
}
