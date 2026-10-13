namespace GitWerka.Core.Mvvm;

using System;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// Base class for simple ViewModels without complex state management.
/// </summary>
public abstract class ViewModelBase : ReactiveObject
{
}

/// <summary>
/// Base class for ViewModels with immutable state management.
/// </summary>
/// <typeparam name="TState">The immutable record representing view state.</typeparam>
public abstract partial class ViewModelBase<TState> : ViewModelBase where TState : class
{
    [Reactive]
    private TState _state;

    protected ViewModelBase(TState initialState)
    {
        _state = initialState;
    }

    /// <summary>
    /// Updates the state using an update function and notifies observers.
    /// </summary>
    /// <param name="updateFunction">Transformation function for state record.</param>
    protected void UpdateState(Func<TState, TState> updateFunction)
    {
        State = updateFunction(State);
    }
}
