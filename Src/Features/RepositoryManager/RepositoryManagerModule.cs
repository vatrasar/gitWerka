namespace GitWerka.Features.RepositoryManager;

using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using GitWerka.Infrastructure.Navigation;
using ReactiveUI;
using Splat;

/// <summary>
/// Feature module registering views and routes for RepositoryManager.
/// </summary>
public class RepositoryManagerModule : IFeatureModule
{
    /// <summary>
    /// Registers view-to-viewmodel associations in Splat dependency resolver.
    /// Usages: AppBootstrapper.Initialize.
    /// </summary>
    /// <param name="services">Mutable dependency resolver from Splat/ReactiveUI.</param>
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new RepositoryManagerView(), typeof(IViewFor<RepositoryManagerViewModel>));
    }
}
