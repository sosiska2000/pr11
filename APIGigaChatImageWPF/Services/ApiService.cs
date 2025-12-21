using APIGigaChatImageWPF.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APIGigaChatImageWPF.Services
{
    public class ApiService
    {
        private HttpClient _httpClient;
        private string _accessToken;
        private readonly string _clientId;
        private readonly string _authKey;

        public ApiService()
        {
            // Используем рабочие ключи из второго приложения
            _clientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";
            _authKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOmUwYWM0NzQwLWFlNTktNDhkMS04NDcwLWRmZjgxNzY3N2M3MQ==";

            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
        }

        public async Task<string> GetTokenAsync()
        {
            try
            {
                string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

                Console.WriteLine($"Получение токена с ClientId: {_clientId}");
                Console.WriteLine($"URL: {url}");

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("RqUID", _clientId);
                request.Headers.Add("Authorization", $"Bearer {_authKey}");

                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                };

                request.Content = new FormUrlEncodedContent(data);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                Console.WriteLine($"Статус ответа при получении токена: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Токен успешно получен");

                    var tokenResponse = JsonConvert.DeserializeObject<ResponseToken>(responseContent);

                    _accessToken = tokenResponse.access_token;
                    Console.WriteLine($"Токен получен (длина: {_accessToken?.Length ?? 0} символов)");
                    return _accessToken;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка получения токена: {response.StatusCode}");
                    Console.WriteLine($"Детали: {error}");
                    throw new Exception($"Ошибка получения токена: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение в GetTokenAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка в GetTokenAsync: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateAndSaveImageAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                    await GetTokenAsync();

                string apiUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                using (var client = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                }))
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                    client.DefaultRequestHeaders.Add("X-Client-ID", _clientId);
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
                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseJson = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Получен ответ от API");

                        var data = JObject.Parse(responseJson);
                        string htmlContent = data["choices"]?[0]?["message"]?["content"]?.ToString();

                        if (string.IsNullOrEmpty(htmlContent))
                        {
                            Console.WriteLine("Пустой ответ от нейросети");
                            throw new Exception("Пустой ответ от нейросети");
                        }

                        Console.WriteLine($"HTML ответ (первые 200 символов): {htmlContent.Substring(0, Math.Min(200, htmlContent.Length))}");

                        // Извлекаем URL изображения из HTML
                        var match = Regex.Match(htmlContent, @"src=""([^""]+)""");

                        if (!match.Success)
                        {
                            match = Regex.Match(htmlContent, @"<img[^>]+src=['""]([^'""]+)['""]");

                            if (!match.Success)
                            {
                                Console.WriteLine($"Не найдено изображение в ответе");
                                throw new Exception("Не найдено изображение в ответе");
                            }
                        }

                        string imageId = match.Groups[1].Value;
                        Console.WriteLine($"ID изображения получен: {imageId}");

                        // Формируем URL для скачивания
                        string fileUrl = $"https://gigachat.devices.sberbank.ru/api/v1/files/{imageId}/content";
                        Console.WriteLine($"URL для скачивания: {fileUrl}");

                        // Скачиваем изображение
                        Console.WriteLine($"Скачивание изображения...");
                        var fileResponse = await client.GetAsync(fileUrl);

                        if (fileResponse.IsSuccessStatusCode)
                        {
                            byte[] imageData = await fileResponse.Content.ReadAsByteArrayAsync();
                            Console.WriteLine($"Изображение скачано: {imageData.Length} байт");

                            // Сохраняем в папку Pictures
                            string picturesPath = System.IO.Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                                "GigaChat Wallpapers");

                            if (!System.IO.Directory.Exists(picturesPath))
                                System.IO.Directory.CreateDirectory(picturesPath);

                            string fileName = $"wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                            string filePath = System.IO.Path.Combine(picturesPath, fileName);

                            // Используем синхронную версию для .NET Framework
                            System.IO.File.WriteAllBytes(filePath, imageData);
                            Console.WriteLine($"Изображение сохранено: {filePath}");

                            return filePath;
                        }
                        else
                        {
                            string error = await fileResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"Ошибка скачивания изображения: {fileResponse.StatusCode}");
                            Console.WriteLine($"Детали: {error}");
                            throw new Exception($"Ошибка скачивания изображения: {fileResponse.StatusCode}");
                        }
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ошибка API: {response.StatusCode}");
                        Console.WriteLine($"Детали: {error}");
                        throw new Exception($"Ошибка генерации: {response.StatusCode} - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GenerateAndSaveImageAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка генерации: {ex.Message}", ex);
            }
        }

        // Упрощенный метод для ручного режима
        public async Task<string> GenerateImageAsync(
            string prompt,
            string style = "реализм",
            string colorPalette = "яркая",
            string aspectRatio = "16:9")
        {
            // Строим улучшенный промпт с параметрами
            string enhancedPrompt = $"{prompt}, стиль: {style}, цветовая палитра: {colorPalette}, " +
                                   $"соотношение сторон: {aspectRatio}, высокое качество, детализированное, " +
                                   "профессиональная графика, обои рабочего стола";

            return await GenerateAndSaveImageAsync(enhancedPrompt);
        }
    }
}