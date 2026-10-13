namespace GitWerka.Features.CommitLog.Domain.Enums;

/// <summary>
/// Identifies which remote operation is pending authentication.
/// </summary>
public enum RemoteOperationPending
{
    None,
    Fetch,
    Pull,
    Push
}
