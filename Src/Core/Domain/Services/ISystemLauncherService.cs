namespace GitWerka.Core.Domain.Services;

/// <summary>
/// Service contract for launching external system tools such as file managers, terminals, and code editors.
/// </summary>
public interface ISystemLauncherService
{
    /// <summary>
    /// Opens the specified directory in the native system file manager (e.g., Nautilus, Dolphin, Explorer, Finder).
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="directoryPath">Directory path to open.</param>
    /// <returns>True if the process was launched successfully, otherwise false.</returns>
    bool OpenInFileManager(string directoryPath);

    /// <summary>
    /// Opens the specified directory in the default or first available system terminal emulator.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="directoryPath">Working directory for the new terminal session.</param>
    /// <returns>True if a terminal process was launched successfully, otherwise false.</returns>
    bool OpenInTerminal(string directoryPath);

    /// <summary>
    /// Opens the specified directory in the preferred code editor (e.g. Visual Studio Code).
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="directoryPath">Directory path to open in the editor.</param>
    /// <param name="editorCommand">Optional custom editor command (defaults to "code").</param>
    /// <returns>True if the editor process was launched successfully, otherwise false.</returns>
    bool OpenInEditor(string directoryPath, string? editorCommand = null);
}
