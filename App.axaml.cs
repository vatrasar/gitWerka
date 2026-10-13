namespace GitWerka;

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GitWerka.Infrastructure;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

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
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindowViewModel viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.ViewModel = viewModel;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
    }

    private static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        GitWerkaDbContext dbContext = scope.ServiceProvider.GetRequiredService<GitWerkaDbContext>();
        dbContext.Database.EnsureCreated();
    }
}