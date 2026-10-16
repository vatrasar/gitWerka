namespace GitWerka.Tests.SharedTests.ConvertersTests;

using System.Globalization;
using Avalonia.Media;
using GitWerka.Core.Domain.Enums;
using GitWerka.Shared.Converters;
using Material.Icons;
using Xunit;

public class FileStatusConvertersTests
{
    [Theory]
    [InlineData(FileChangeStatus.Added, MaterialIconKind.Plus)]
    [InlineData(FileChangeStatus.Untracked, MaterialIconKind.Plus)]
    [InlineData(FileChangeStatus.Deleted, MaterialIconKind.Minus)]
    [InlineData(FileChangeStatus.Modified, MaterialIconKind.PencilOutline)]
    [InlineData(FileChangeStatus.Renamed, MaterialIconKind.FileMoveOutline)]
    [InlineData(FileChangeStatus.Copied, MaterialIconKind.ContentCopy)]
    [InlineData(FileChangeStatus.Ignored, MaterialIconKind.EyeOffOutline)]
    [InlineData(FileChangeStatus.Conflicted, MaterialIconKind.AlertCircleOutline)]
    public void FileStatusToIconConverter_ReturnsExpectedIcon(FileChangeStatus status, MaterialIconKind expectedIcon)
    {
        object? result = FileStatusToIconConverter.Instance.Convert(status, typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(expectedIcon, result);
    }

    [Fact]
    public void FileStatusToIconConverter_NonStatusInput_ReturnsDefaultFileOutline()
    {
        object? result = FileStatusToIconConverter.Instance.Convert("invalid", typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(MaterialIconKind.FileOutline, result);
    }

    [Theory]
    [InlineData(FileChangeStatus.Added)]
    [InlineData(FileChangeStatus.Untracked)]
    [InlineData(FileChangeStatus.Deleted)]
    [InlineData(FileChangeStatus.Modified)]
    [InlineData(FileChangeStatus.Conflicted)]
    [InlineData(FileChangeStatus.Renamed)]
    public void FileStatusToBrushConverter_ReturnsNonNullBrush(FileChangeStatus status)
    {
        object? result = FileStatusToBrushConverter.Instance.Convert(status, typeof(IBrush), null, CultureInfo.InvariantCulture);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IBrush>(result);
    }
}
