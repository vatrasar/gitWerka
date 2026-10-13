namespace GitWerka.Features.CommitLog;

using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Infrastructure.Navigation;
using ReactiveUI;
using Splat;

/// <summary>
/// Feature module registering views and routes for CommitLog.
/// </summary>
public class CommitLogModule : IFeatureModule
{
    /// <summary>
    /// Registers view-to-viewmodel associations in Splat dependency resolver.
    /// Usages: AppBootstrapper.Initialize.
    /// </summary>
    /// <param name="services">Mutable dependency resolver from Splat/ReactiveUI.</param>
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new CommitLogView(), typeof(IViewFor<CommitLogViewModel>));
    }
}
