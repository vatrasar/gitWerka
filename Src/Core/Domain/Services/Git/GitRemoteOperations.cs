namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for Git remote repositories, network fetch/pull/push, and Git author configuration.
/// </summary>
public class GitRemoteOperations
{
    /// <summary>
    /// Reads all configured remote repositories.
    /// </summary>
    public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            List<RemoteInfo> remotes = repository.Network.Remotes
                .Select(r => new RemoteInfo { Name = r.Name, Url = r.Url, PushUrl = r.PushUrl })
                .ToList();

            return (IReadOnlyList<RemoteInfo>)remotes;
        }, cancellationToken);
    }

    /// <summary>
    /// Adds a new remote repository to the configuration.
    /// </summary>
    public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Network.Remotes.Add(remoteName, url);
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a remote repository from the configuration.
    /// </summary>
    public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Network.Remotes.Remove(remoteName);
        }, cancellationToken);
    }

    /// <summary>
    /// Fetches changes from remotes.
    /// </summary>
    public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            FetchRemotesInternal(repository, remoteName, credentials);
        }, cancellationToken);
    }

    /// <summary>
    /// Pulls changes from the upstream tracking branch into the current HEAD branch.
    /// </summary>
    public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            FetchRemotesInternal(repository, remoteName, credentials);

            PullOptions options = new()
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = BuildCredentialsHandler(credentials)
                }
            };

            Signature merger = GitServiceHelper.BuildSignatureOrDefault(repository);
            Commands.Pull(repository, merger, options);
        }, cancellationToken);
    }

    /// <summary>
    /// Pushes the current HEAD branch to the specified remote.
    /// </summary>
    public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            PushBranchInternal(repository, remoteName, credentials);
        }, cancellationToken);
    }

    private static void FetchRemotesInternal(Repository repo, string? remoteName, GitCredentials? credentials)
    {
        var remotes = string.IsNullOrWhiteSpace(remoteName)
            ? repo.Network.Remotes.ToList()
            : repo.Network.Remotes.Where(r => r.Name == remoteName).ToList();

        FetchOptions options = new()
        {
            CredentialsProvider = BuildCredentialsHandler(credentials)
        };

        foreach (Remote remote in remotes)
        {
            Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(s => s.Specification), options, null);
        }
    }

    private static void PushBranchInternal(Repository repo, string? remoteName, GitCredentials? credentials)
    {
        Remote? remote = string.IsNullOrWhiteSpace(remoteName)
            ? (repo.Network.Remotes["origin"] ?? repo.Network.Remotes.FirstOrDefault())
            : repo.Network.Remotes[remoteName];

        if (remote != null && repo.Head != null)
        {
            PushOptions options = new()
            {
                CredentialsProvider = BuildCredentialsHandler(credentials)
            };
            repo.Network.Push(remote, repo.Head.CanonicalName, options);
        }
    }

    private static LibGit2Sharp.Handlers.CredentialsHandler? BuildCredentialsHandler(GitCredentials? credentials)
    {
        if (credentials == null)
        {
            return null;
        }

        return (url, userFromUrl, types) =>
        {
            if (credentials.Type == GitCredentialType.SshKeyPassphrase)
            {
                if (!string.IsNullOrWhiteSpace(credentials.Passphrase))
                {
                    return new UsernamePasswordCredentials
                    {
                        Username = string.IsNullOrWhiteSpace(credentials.Username) ? userFromUrl : credentials.Username,
                        Password = credentials.Passphrase
                    };
                }

                return new DefaultCredentials();
            }

            return new UsernamePasswordCredentials
            {
                Username = string.IsNullOrWhiteSpace(credentials.Username) ? userFromUrl : credentials.Username,
                Password = credentials.PasswordOrToken
            };
        };
    }

    /// <summary>
    /// Retrieves user name and email from repository configuration, falling back to global configuration.
    /// </summary>
    public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            ConfigurationEntry<string>? nameEntry = repository.Config.Get<string>("user.name");
            ConfigurationEntry<string>? emailEntry = repository.Config.Get<string>("user.email");

            return new GitConfigInfo
            {
                UserName = nameEntry?.Value ?? string.Empty,
                UserEmail = emailEntry?.Value ?? string.Empty
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Sets user name and email in the local repository configuration.
    /// </summary>
    public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Config.Set("user.name", userName, ConfigurationLevel.Local);
            repository.Config.Set("user.email", userEmail, ConfigurationLevel.Local);
        }, cancellationToken);
    }
}
