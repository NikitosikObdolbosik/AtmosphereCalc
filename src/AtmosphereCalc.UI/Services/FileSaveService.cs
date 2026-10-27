using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AtmosphereCalc.Services;

/// <summary>
/// Реализация <see cref="IFileSaveService"/> на системном диалоге Avalonia.
/// </summary>
public sealed class FileSaveService(TopLevel topLevel) : IFileSaveService
{
    public Task<string?> SaveTextAsync(string suggestedFileName, string content) =>
        SaveAsync(suggestedFileName, async stream =>
        {
            // BOM нужен, чтобы Excel правильно показал кириллицу.
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await writer.WriteAsync(content);
        });

    public Task<string?> SaveBytesAsync(string suggestedFileName, byte[] content) =>
        SaveAsync(suggestedFileName, stream => stream.WriteAsync(content).AsTask());

    private async Task<string?> SaveAsync(string suggestedFileName, Func<Stream, Task> write)
    {
        string extension = Path.GetExtension(suggestedFileName).TrimStart('.');

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспорт",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(extension.ToUpperInvariant()) { Patterns = [$"*.{extension}"] }]
        });

        if (file is null) return null;

        await using var stream = await file.OpenWriteAsync();
        await write(stream);

        return file.Name;
    }
}
