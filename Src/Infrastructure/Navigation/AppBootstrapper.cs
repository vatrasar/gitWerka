namespace GitWerka.Infrastructure.Navigation;

using System;
using System.Linq;
using System.Reflection;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Splat;

/// <summary>
/// Bootstraps application modules and navigation registrations.
/// </summary>
public class AppBootstrapper
{
    /// <summary>
    /// Initializes application feature modules and ensures correct ReactiveUI UI thread scheduler.
    /// </summary>
    public static void Initialize()
    {
        IMutableDependencyResolver resolver = Locator.CurrentMutable;
        Assembly assembly = typeof(AppBootstrapper).Assembly;

        var moduleTypes = assembly.GetTypes()
            .Where(type => typeof(IFeatureModule).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        foreach (Type moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType) is IFeatureModule module)
            {
                module.Register(resolver);
            }
        }

        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
    }
}
