namespace GitWerka.Infrastructure;

using GitWerka.Core.Config;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using GitWerka.Features.Shell.UI.Screens.Host;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Data.Repositories;
using GitWerka.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection configuration and extension methods for GitWerka.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures application services, repositories, configuration, and view models.
    /// Usages: App.OnFrameworkInitializationCompleted.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>Configured service collection.</returns>
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppConfig>(configuration.GetSection("AppConfig"));

        services.AddDbContext<GitWerkaDbContext>(options =>
        {
        });

        services.AddScoped<IRecentRepositoriesRepository, RecentRepositoriesRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IAiConfigurationRepository, AiConfigurationRepository>();
        services.AddScoped<IApplicationSettingsRepository, ApplicationSettingsRepository>();
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<ISystemLauncherService, SystemLauncherService>();
        services.AddSingleton<System.Net.Http.HttpClient>();
        services.AddScoped<IAiCommitMessageService, OpenRouterAiCommitMessageService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<HostViewModel>();
        services.AddTransient<RepositoryManagerViewModel>();
        services.AddTransient<GitWerka.Features.CommitLog.UI.Screens.CommitLog.CommitLogViewModel>();
        services.AddTransient<GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement.ProfileManagementViewModel>();

        return services;
    }
}
