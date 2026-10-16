namespace GitWerka.Features.MergeRebase;

using GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;
using GitWerka.Infrastructure.Navigation;
using ReactiveUI;
using Splat;

/// <summary>
/// Feature module registering views and routes for MergeRebase and conflict resolution.
/// </summary>
public class MergeRebaseModule : IFeatureModule
{
    /// <summary>
    /// Registers view-to-viewmodel associations in Splat dependency resolver.
    /// Usages: AppBootstrapper.Initialize.
    /// </summary>
    /// <param name="services">Mutable dependency resolver from Splat/ReactiveUI.</param>
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new ConflictResolverView(), typeof(IViewFor<ConflictResolverViewModel>));
    }
}
