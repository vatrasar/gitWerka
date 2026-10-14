namespace GitWerka.Features.ProfileManagement;

using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using GitWerka.Infrastructure.Navigation;
using ReactiveUI;
using Splat;

/// <summary>
/// Feature module registering views and routes for ProfileManagement.
/// </summary>
public class ProfileManagementModule : IFeatureModule
{
    /// <summary>
    /// Registers view-to-viewmodel associations in Splat dependency resolver.
    /// Usages: AppBootstrapper.Initialize.
    /// </summary>
    /// <param name="services">Mutable dependency resolver from Splat/ReactiveUI.</param>
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new ProfileManagementView(), typeof(IViewFor<ProfileManagementViewModel>));
    }
}
