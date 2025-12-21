using APIGigaChatImageWPF.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APIGigaChatImage
{
    public class Program
    {
        // Используем рабочие ключи из второго приложения
        public static string ClientId = "019b287d-4c6f-7695-97bd-095b75ac26a5";
        public static string AutorizationKey = "MDE5YjI4N2QtNGM2Zi03Njk1LTk3YmQtMDk1Yjc1YWMyNmE1OmJkMjI4NGU2LWFlYzctNDg0Ny1hM2FkLTg0NGViZjY2NzFlNA==";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Генератор изображений GigaChat");
            Console.WriteLine("================================");

            try
            {
                // Шаг 1: Получаем токен
                Console.WriteLine("Получение токена...");
                string token = await GetToken(ClientId, AutorizationKey);

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Ошибка: Не удалось получить токен");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"✓ Токен получен: {token.Substring(0, Math.Min(30, token.Length))}...");
                Console.WriteLine();

                // Шаг 2: Генерация изображения
                Console.Write("Введите описание для генерации изображения: ");
                string prompt = Console.ReadLine();

                if (string.IsNullOrEmpty(prompt))
                {
                    prompt = "Красивый закат в горах, цифровое искусство";
                    Console.WriteLine($"Используется промпт по умолчанию: {prompt}");
                }

                Console.WriteLine();
                Console.WriteLine("Генерация изображения...");

                // Генерируем изображение
                string imagePath = await GenerateAndSaveImageAsync(token, prompt);

                if (!string.IsNullOrEmpty(imagePath))
                {
                    Console.WriteLine($"✓ Изображение сгенерировано и сохранено!");
                    Console.WriteLine($"  Путь: {imagePath}");

                    // Проверяем размер файла
                    if (File.Exists(imagePath))
                    {
                        FileInfo fileInfo = new FileInfo(imagePath);
                        Console.WriteLine($"  Размер файла: {fileInfo.Length} байт ({(fileInfo.Length / 1024):N0} KB)");

                        // Предлагаем установить обои
                        Console.WriteLine();
                        Console.Write("Хотите установить изображение как обои? (y/n): ");
                        string answer = Console.ReadLine();

                        if (answer?.ToLower() == "y" || answer?.ToLower() == "д")
                        {
                            try
                            {
                                SetWallpaper(imagePath);
                                Console.WriteLine("✓ Обои успешно установлены!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"✗ Ошибка установки обоев: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("✗ Не удалось создать изображение");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Произошла ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        // Метод для получения токена
        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string returnToken = null;
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                // Отключаем проверку SSL для тестового сервера
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("RqUID", rqUID);
                        request.Headers.Add("Authorization", $"Bearer {bearer}");

                        var data = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                        };

                        request.Content = new FormUrlEncodedContent(data);

                        Console.WriteLine($"Отправка запроса на {url}");
                        HttpResponseMessage response = await client.SendAsync(request);

                        Console.WriteLine($"Статус ответа: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"✓ Токен получен успешно");

                            ResponseToken token = JsonConvert.DeserializeObject<ResponseToken>(responseContent);
                            returnToken = token.access_token;
                        }
                        else
                        {
                            string errorContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"✗ Ошибка получения токена: {response.StatusCode}");
                            Console.WriteLine($"Детали ошибки: {errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Исключение при получении токена: {ex.Message}");
                    }
                }
            }
            return returnToken;
        }

        // Улучшенный метод для генерации и сохранения изображения
        public static async Task<string> GenerateAndSaveImageAsync(string token, string prompt)
        {
            try
            {
                string apiUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        client.DefaultRequestHeaders.Add("X-Client-ID", ClientId);
                        client.Timeout = TimeSpan.FromSeconds(120);

                        var requestData = new
                        {
                            model = "GigaChat",
                            messages = new[]
                            {
                                new { role = "user", content = prompt }
                            },
                            function_call = "auto"
                        };

                        string json = JsonConvert.SerializeObject(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        Console.WriteLine($"Отправка запроса в GigaChat...");
                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                        Console.WriteLine($"Статус ответа: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            string responseJson = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"✓ Получен ответ от API");

                            // Парсим HTML ответ для получения ссылки на изображение
                            var data = JObject.Parse(responseJson);
                            string htmlContent = data["choices"]?[0]?["message"]?["content"]?.ToString();

                            if (string.IsNullOrEmpty(htmlContent))
                            {
                                Console.WriteLine("✗ Пустой ответ от нейросети");
                                return null;
                            }

                            Console.WriteLine($"HTML ответ (первые 200 символов): {htmlContent.Substring(0, Math.Min(200, htmlContent.Length))}");

                            // Извлекаем URL изображения из HTML
                            var match = Regex.Match(htmlContent, @"src=""([^""]+)""");

                            if (!match.Success)
                            {
                                match = Regex.Match(htmlContent, @"<img[^>]+src=['""]([^'""]+)['""]");

                                if (!match.Success)
                                {
                                    Console.WriteLine("✗ Не найдено изображение в ответе");
                                    return null;
                                }
                            }

                            string imageId = match.Groups[1].Value;
                            Console.WriteLine($"✓ ID изображения получен: {imageId}");

                            // Формируем URL для скачивания
                            string fileUrl = $"https://gigachat.devices.sberbank.ru/api/v1/files/{imageId}/content";
                            Console.WriteLine($"URL для скачивания: {fileUrl}");

                            // Скачиваем изображение
                            Console.WriteLine($"Скачивание изображения...");
                            var fileResponse = await client.GetAsync(fileUrl);

                            if (fileResponse.IsSuccessStatusCode)
                            {
                                byte[] imageData = await fileResponse.Content.ReadAsByteArrayAsync();
                                Console.WriteLine($"✓ Изображение скачано: {imageData.Length} байт");

                                // Сохраняем в текущую директорию
                                string fileName = $"generated_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                                string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

                                File.WriteAllBytes(filePath, imageData);
                                Console.WriteLine($"✓ Изображение сохранено: {filePath}");

                                return filePath;
                            }
                            else
                            {
                                string error = await fileResponse.Content.ReadAsStringAsync();
                                Console.WriteLine($"✗ Ошибка скачивания изображения: {fileResponse.StatusCode}");
                                Console.WriteLine($"Детали: {error}");
                                return null;
                            }
                        }
                        else
                        {
                            string error = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"✗ Ошибка API: {response.StatusCode}");
                            Console.WriteLine($"Детали: {error}");
                            return null;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("✗ Превышено время ожидания. Попробуйте упростить запрос.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка создания изображения: {ex.Message}");
                return null;
            }
        }

        // Метод для установки обоев
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SystemParametersInfo(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        private static void SetWallpaper(string imagePath)
        {
            try
            {
                SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    imagePath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                Console.WriteLine($"✓ Обои установлены: {imagePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка: {ex.Message}");
                throw;
            }
        }
    }

    // Класс для десериализации токена (если нужно в отдельном файле)
    public class ResponseToken
    {
        public string access_token { get; set; }
        public string expires_at { get; set; }
    }
}