namespace GitWerka;

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Infrastructure;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        IConfiguration configuration = BuildConfiguration();
        IServiceCollection services = new ServiceCollection();
        services.AddAppServices(configuration);

        _serviceProvider = services.BuildServiceProvider();

        InitializeDatabase(_serviceProvider);
        AppBootstrapper.Initialize();
        RegisterSplatServices(_serviceProvider);
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindowViewModel viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.ViewModel = viewModel;
            desktop.MainWindow = mainWindow;

            string? targetDirectory = ParseDirectoryArgument(desktop.Args);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                viewModel.OpenInitialRepository(targetDirectory);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string? ParseDirectoryArgument(string[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return null;
        }

        foreach (string rawArg in args)
        {
            string? directoryPath = TryExtractDirectoryPath(rawArg);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                return directoryPath;
            }
        }

        return null;
    }

    private static string? TryExtractDirectoryPath(string rawArg)
    {
        if (string.IsNullOrWhiteSpace(rawArg))
        {
            return null;
        }

        string arg = rawArg.Trim().Trim('"', '\'');
        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            return null;
        }

        string candidatePath = NormalizePathArgument(arg);
        return ValidateDirectoryOrFilePath(candidatePath);
    }

    private static string NormalizePathArgument(string arg)
    {
        if (!arg.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return arg;
        }

        if (Uri.TryCreate(arg, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return Uri.UnescapeDataString(arg.Substring(7));
    }

    private static string? ValidateDirectoryOrFilePath(string candidatePath)
    {
        try
        {
            string fullPath = Path.GetFullPath(candidatePath);
            if (Directory.Exists(fullPath) || File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse path '{candidatePath}': {ex.Message}");
        }

        return null;
    }

    private static void RegisterSplatServices(IServiceProvider serviceProvider)
    {
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IAiCommitMessageService>(), typeof(IAiCommitMessageService));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IAiConfigurationRepository>(), typeof(IAiConfigurationRepository));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IProfileRepository>(), typeof(IProfileRepository));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IRecentRepositoriesRepository>(), typeof(IRecentRepositoriesRepository));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IApplicationSettingsRepository>(), typeof(IApplicationSettingsRepository));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IGitService>(), typeof(IGitService));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<ISystemLauncherService>(), typeof(ISystemLauncherService));
        Locator.CurrentMutable.Register(() => serviceProvider.GetService<IAppStatusService>(), typeof(IAppStatusService));
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();
    }

    private static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        GitWerkaDbContext dbContext = scope.ServiceProvider.GetRequiredService<GitWerkaDbContext>();
        dbContext.Database.EnsureCreated();

        TryApplyDatabaseMigrations(dbContext);

        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Authors (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ProfileCredentials (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                AccessToken TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Profiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                AuthorId INTEGER NOT NULL,
                CredentialsId INTEGER NOT NULL,
                FOREIGN KEY (AuthorId) REFERENCES Authors(Id) ON DELETE RESTRICT,
                FOREIGN KEY (CredentialsId) REFERENCES ProfileCredentials(Id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS RepositoryProfiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RepositoryPath TEXT NOT NULL UNIQUE,
                ProfileId INTEGER NOT NULL,
                FOREIGN KEY (ProfileId) REFERENCES Profiles(Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS AiConfigurations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ApiKey TEXT NULL,
                Model TEXT NOT NULL,
                CommitStyle TEXT NOT NULL,
                TokenLimit INTEGER NOT NULL,
                BaseUrl TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS RepositoryAiContexts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RepositoryPath TEXT NOT NULL UNIQUE,
                ProjectDescription TEXT NULL
            );
            CREATE TABLE IF NOT EXISTS ApplicationSettings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DefaultProjectsDirectory TEXT NOT NULL DEFAULT ''
            );
        ");
    }

    private static void TryApplyDatabaseMigrations(GitWerkaDbContext dbContext)
    {
        TryExecuteMigrationSql(dbContext, "ALTER TABLE RecentRepositories ADD COLUMN IsFavorite INTEGER NOT NULL DEFAULT 0;");
        TryExecuteMigrationSql(dbContext, "ALTER TABLE RecentRepositories ADD COLUMN CustomName TEXT NULL;");
    }

    private static void TryExecuteMigrationSql(GitWerkaDbContext dbContext, string sql)
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Migration statement skipped: {ex.Message}");
        }
    }
}