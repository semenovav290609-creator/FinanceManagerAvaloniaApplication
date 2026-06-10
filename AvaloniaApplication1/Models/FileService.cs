using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AvaloniaApplication1.ViewModels
{
    public static class FileService
    {
        // Асинхронное сохранение данных в JSON файл
        public static async Task SaveAsync<T>(string fileName, T data)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    // Включение поддержки кодировки Unicode для кириллицы
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
                };

                string json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(fileName, json);
            }
            catch
            {
                // Игнорирование ошибок записи
            }
        }

        // Асинхронная загрузка данных из JSON файла
        public static async Task<T?> LoadAsync<T>(string fileName)
        {
            if (!File.Exists(fileName)) return default;
            try
            {
                string json = await File.ReadAllTextAsync(fileName);

                // Десериализация без учета регистра символов в именах свойств
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch
            {
                return default;
            }
        }

        // Безопасное удаление файла с диска
        public static void DeleteFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch
            {
                // Игнорирование ошибок удаления
            }
        }
    }
}
