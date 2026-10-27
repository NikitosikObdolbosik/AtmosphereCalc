using System.Threading.Tasks;

namespace AtmosphereCalc.Services;

/// <summary>
/// Сохранение файла через диалог. ViewModel зависит только от этого интерфейса,
/// поэтому её можно тестировать без окна.
/// Методы возвращают имя сохранённого файла или null, если пользователь отменил сохранение.
/// Тип файла в диалоге определяется по расширению <c>suggestedFileName</c>.
/// </summary>
public interface IFileSaveService
{
    Task<string?> SaveTextAsync(string suggestedFileName, string content);

    Task<string?> SaveBytesAsync(string suggestedFileName, byte[] content);
}
