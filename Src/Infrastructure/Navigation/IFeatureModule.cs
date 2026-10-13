namespace GitWerka.Infrastructure.Navigation;

using Splat;

/// <summary>
/// Defines contract for feature module registrations.
/// </summary>
public interface IFeatureModule
{
    /// <summary>
    /// Registers views and navigation dependencies for the feature.
    /// </summary>
    /// <param name="services">Mutable dependency resolver from Splat/ReactiveUI.</param>
    void Register(IMutableDependencyResolver services);
}
