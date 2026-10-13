namespace GitWerka.Infrastructure.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using GitWerka.Core.Domain.Services;

/// <summary>
/// Service implementation for launching native system file managers, terminals, and code editors.
/// </summary>
public class SystemLauncherService : ISystemLauncherService
{
    private static readonly string[] LinuxTerminals =
    {
        "x-terminal-emulator",
        "gnome-terminal",
        "konsole",
        "xfce4-terminal",
        "mate-terminal",
        "alacritty",
        "kitty",
        "tilix",
        "xterm"
    };

    private static readonly string[] LinuxFileManagers =
    {
        "xdg-open",
        "nautilus",
        "dolphin",
        "thunar",
        "pcmanfm"
    };

    private static readonly string[] DefaultEditors =
    {
        "code",
        "vscodium",
        "subl",
        "gedit",
        "kate"
    };

    /// <summary>
    /// Opens the specified directory in the native system file manager.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="directoryPath">Directory path to open.</param>
    /// <returns>True if file manager opened successfully, otherwise false.</returns>
    public bool OpenInFileManager(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return TryStartProcess("explorer.exe", $"\"{directoryPath}\"", null);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return TryStartProcess("open", $"\"{directoryPath}\"", null);
        }

        return TryLaunchFromCandidates(LinuxFileManagers, directoryPath, null);
    }

    /// <summary>
    /// Opens the specified directory in the default or first available system terminal emulator.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="directoryPath">Working directory for the new terminal session.</param>
    /// <returns>True if terminal was launched successfully, otherwise false.</returns>
    public bool OpenInTerminal(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return TryStartProcess("cmd.exe", string.Empty, directoryPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return TryStartProcess("open", $"-a Terminal \"{directoryPath}\"", null);
        }

        return LaunchLinuxTerminal(directoryPath);
    }

    /// <summary>
    /// Opens the specified directory in the preferred code editor.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="directoryPath">Directory path to open in the editor.</param>
    /// <param name="editorCommand">Optional custom editor command.</param>
    /// <returns>True if editor was launched successfully, otherwise false.</returns>
    public bool OpenInEditor(string directoryPath, string? editorCommand = null)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(editorCommand))
        {
            return TryStartProcess(editorCommand, $"\"{directoryPath}\"", directoryPath);
        }

        return TryLaunchFromCandidates(DefaultEditors, $"\"{directoryPath}\"", directoryPath);
    }

    private static bool LaunchLinuxTerminal(string directoryPath)
    {
        foreach (string term in LinuxTerminals)
        {
            string arguments = GetLinuxTerminalArguments(term, directoryPath);
            if (TryStartProcess(term, arguments, directoryPath))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetLinuxTerminalArguments(string terminalCommand, string directoryPath) => terminalCommand switch
    {
        "gnome-terminal" or "xfce4-terminal" or "mate-terminal" or "tilix" => $"--working-directory=\"{directoryPath}\"",
        "konsole" => $"--workdir \"{directoryPath}\"",
        _ => string.Empty
    };

    private static bool TryLaunchFromCandidates(string[] commands, string arguments, string? workingDirectory)
    {
        foreach (string cmd in commands)
        {
            if (TryStartProcess(cmd, arguments, workingDirectory))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryStartProcess(string fileName, string arguments, string? workingDirectory)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            using Process? process = Process.Start(startInfo);
            return process != null;
        }
        catch
        {
            return false;
        }
    }
}
