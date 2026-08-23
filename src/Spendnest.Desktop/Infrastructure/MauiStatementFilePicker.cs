using Spendnest.Desktop.Services;

namespace Spendnest.Desktop.Infrastructure;

public sealed class MauiStatementFilePicker : IStatementFilePicker
{
    public async Task<PickedStatementFile?> PickCsvAsync(CancellationToken cancellationToken)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a statement CSV",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = [".csv"]
            })
        });

        if (file is null)
        {
            return null;
        }

        var localPath = await EnsureLocalPathAsync(file);
        return new PickedStatementFile(file.FileName, localPath);
    }

    private static async Task<string> EnsureLocalPathAsync(FileResult file)
    {
        if (!string.IsNullOrWhiteSpace(file.FullPath) && File.Exists(file.FullPath))
        {
            return file.FullPath;
        }

        var cachePath = Path.Combine(FileSystem.CacheDirectory, file.FileName);
        await using var source = await file.OpenReadAsync();
        await using var destination = File.Create(cachePath);
        await source.CopyToAsync(destination);
        return cachePath;
    }
}
