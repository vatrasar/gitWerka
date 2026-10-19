namespace GitWerka.Core.Domain.Models;

using GitWerka.Core.Domain.Enums;

public record AppStatus(
    string Message,
    AppStatusSeverity Severity = AppStatusSeverity.Ready
);
