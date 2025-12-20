using APIGigaChatImage.Models.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage
{
    public class Program
    {
        public static string ClientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";
        public static string AutorizationKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOjdlMmI5OGMyLWRkMDAtNDQ1OC04NjdiLTUwMGUxMzNjNjIxMQ==";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Генератор изображений GigaChat");
            Console.WriteLine();

            try
            {
                // Шаг 1: Получаем токен
                Console.WriteLine("Получение токена...");
                string token = await GetToken(ClientId, AutorizationKey);

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Ошибка: Не удалось получить токен");
                    return;
                }

                Console.WriteLine($"Токен получен: {token.Substring(0, Math.Min(30, token.Length))}...");
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

                // Вызываем метод по заданию (токен + промпт)
                string imageId = await GenerateImageAsync(token, prompt);

                if (!string.IsNullOrEmpty(imageId))
                {
                    Console.WriteLine($"✓ Изображение сгенерировано! ID: {imageId}");

                    // Скачиваем изображение
                    Console.WriteLine();
                    Console.WriteLine("Скачивание изображения...");
                    string fileName = $"generated_{DateTime.Now:yyyyMMddHHmmss}.jpg";

                    bool downloaded = await DownloadImageAsync(token, imageId, fileName);

                    if (downloaded)
                    {
                        Console.WriteLine($"✓ Изображение успешно скачано: {fileName}");

                        // Проверяем размер файла
                        if (File.Exists(fileName))
                        {
                            FileInfo fileInfo = new FileInfo(fileName);
                            Console.WriteLine($"Размер файла: {fileInfo.Length} байт");

                            // Предлагаем установить обои
                            Console.WriteLine();
                            Console.Write("Хотите установить изображение как обои? (y/n): ");
                            string answer = Console.ReadLine();

                            if (answer?.ToLower() == "y")
                            {
                                try
                                {
                                    // Используем WallpaperSetter из WPF проекта
                                    SetWallpaper(fileName);
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
                        Console.WriteLine("✗ Не удалось скачать изображение");
                    }
                }
                else
                {
                    Console.WriteLine("✗ Ошибка при генерации изображения");
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
                // Важно: отключаем проверку SSL для тестового сервера
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
                            Console.WriteLine($"Ответ сервера: {responseContent}");

                            ResponseToken token = JsonConvert.DeserializeObject<ResponseToken>(responseContent);
                            returnToken = token.access_token;
                        }
                        else
                        {
                            string errorContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Ошибка получения токена: {response.StatusCode}");
                            Console.WriteLine($"Детали ошибки: {errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Исключение при получении токена: {ex.Message}");
                    }
                }
            }
            return returnToken;
        }

        // Метод для генерации изображения
        public static async Task<string> GenerateImageAsync(string token, string prompt)
        {
            string imageId = null;

            // Пробуем разные URL API
            string[] apiUrls = {
                "https://gigachat.devices.sberbank.ru/api/v1/images/generations",
                "https://developers.sber.ru/gigachat/api/v1/images/generations"
            };

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    foreach (string url in apiUrls)
                    {
                        try
                        {
                            Console.WriteLine($"Попытка подключения к: {url}");

                            var requestData = new
                            {
                                model = "GigaChat",
                                prompt = prompt,
                                n = 1,
                                size = "1024x1024"
                            };

                            string json = JsonConvert.SerializeObject(requestData);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            // Добавляем заголовки
                            client.DefaultRequestHeaders.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                            client.DefaultRequestHeaders.Add("Accept", "application/json");

                            // Пробуем добавить дополнительные заголовки
                            client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

                            Console.WriteLine($"Отправка запроса с промптом: {prompt}");
                            HttpResponseMessage response = await client.PostAsync(url, content);

                            Console.WriteLine($"Статус ответа: {response.StatusCode}");

                            if (response.IsSuccessStatusCode)
                            {
                                string responseContent = await response.Content.ReadAsStringAsync();
                                Console.WriteLine($"Успешный ответ от {url}");

                                var imageResponse = JsonConvert.DeserializeObject<ImageGenerationResponse>(responseContent);

                                if (imageResponse?.data?.Count > 0)
                                {
                                    imageId = imageResponse.data[0].id;
                                    Console.WriteLine($"ID изображения: {imageId}");
                                    Console.WriteLine($"URL изображения: {imageResponse.data[0].url}");
                                    break; // Успешно, выходим из цикла
                                }
                            }
                            else
                            {
                                string errorContent = await response.Content.ReadAsStringAsync();
                                Console.WriteLine($"Ошибка от {url}: {response.StatusCode}");
                                Console.WriteLine($"Детали: {errorContent}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Исключение при подключении к {url}: {ex.Message}");
                        }
                    }
                }
            }

            return imageId;
        }

        // Метод для скачивания изображения
        public static async Task<bool> DownloadImageAsync(string token, string imageId, string filePath)
        {
            try
            {
                Console.WriteLine($"Скачивание изображения ID: {imageId}");

                // Пробуем разные методы получения URL
                string[] infoUrls = {
                    $"https://gigachat.devices.sberbank.ru/api/v1/images/{imageId}",
                    $"https://developers.sber.ru/gigachat/api/v1/images/{imageId}"
                };

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        string imageUrl = null;

                        foreach (string infoUrl in infoUrls)
                        {
                            try
                            {
                                client.DefaultRequestHeaders.Authorization =
                                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                                Console.WriteLine($"Запрос информации: {infoUrl}");
                                HttpResponseMessage infoResponse = await client.GetAsync(infoUrl);

                                if (infoResponse.IsSuccessStatusCode)
                                {
                                    string infoContent = await infoResponse.Content.ReadAsStringAsync();
                                    var imageInfo = JsonConvert.DeserializeObject<ImageInfoResponse>(infoContent);

                                    if (!string.IsNullOrEmpty(imageInfo?.url))
                                    {
                                        imageUrl = imageInfo.url;
                                        Console.WriteLine($"URL для скачивания найден: {imageUrl}");
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка запроса информации: {ex.Message}");
                            }
                        }

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // Скачиваем изображение
                            byte[] imageData = await client.GetByteArrayAsync(imageUrl);

                            if (imageData != null && imageData.Length > 0)
                            {
                                File.WriteAllBytes(filePath, imageData);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка скачивания: {ex.Message}");
            }

            return false;
        }

        // Метод для установки обоев (аналогично WPF)
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
            SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                imagePath,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }

    // Классы для десериализации
    public class ImageGenerationResponse
    {
        public long created { get; set; }
        public List<ImageData> data { get; set; }

        public class ImageData
        {
            public string url { get; set; }
            public string id { get; set; }
        }
    }

    public class ImageInfoResponse
    {
        public string id { get; set; }
        public string url { get; set; }
        public string status { get; set; }
    }
}