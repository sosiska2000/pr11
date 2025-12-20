using APIGigaChatImage.Models.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace APIGigaChatImageWPF.Services
{
    public class ApiService
    {
        private HttpClient _httpClient;
        private string _accessToken;

        public ApiService()
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
        }

        public async Task<string> GetTokenAsync()
        {
            try
            {
                // Пробуем получить настройки из конфигурации
                string clientId = ConfigurationManager.AppSettings["ClientId"];
                string authKey = ConfigurationManager.AppSettings["AuthorizationKey"];
                string url = ConfigurationManager.AppSettings["AuthUrl"];

                // Если не получили из конфига, используем значения из консольного приложения
                if (string.IsNullOrEmpty(clientId))
                    clientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";

                if (string.IsNullOrEmpty(authKey))
                    authKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOmZiYjEwNTdmLWM2ZmUtNDAwYS04NThjLTNlMTA2NjRmYTVkMA==";

                if (string.IsNullOrEmpty(url))
                    url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

                Console.WriteLine($"Получение токена с ClientId: {clientId}");
                Console.WriteLine($"URL: {url}");

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("RqUID", clientId);
                request.Headers.Add("Authorization", $"Bearer {authKey}");

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
                    Console.WriteLine($"Длина токена: {_accessToken?.Length ?? 0} символов");
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

        private string GetSetting(string key)
        {
            try
            {
                // Способ 1: Через ConfigurationManager
                return ConfigurationManager.AppSettings[key];
            }
            catch
            {
                try
                {
                    // Способ 2: Через AppDomain
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    return config.AppSettings.Settings[key]?.Value;
                }
                catch
                {
                    return null;
                }
            }
        }

        public async Task<ImageGenerationResponse> GenerateImageAsync(
    string prompt,
    string style = "realistic",
    string colorPalette = "vibrant",
    string aspectRatio = "16:9")
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    Console.WriteLine("Токен не найден, получаем новый...");
                    await GetTokenAsync();
                }

                Console.WriteLine($"Генерация изображения с промптом: {prompt}");
                Console.WriteLine($"Токен доступен: {!string.IsNullOrEmpty(_accessToken)}");
                Console.WriteLine($"Длина токена: {_accessToken?.Length ?? 0} символов");

                var requestData = new
                {
                    model = "GigaChat:latest",
                    prompt = prompt,
                    n = 1,
                    size = "1024x1024",
                    style = style,
                    color_palette = colorPalette,
                    aspect_ratio = aspectRatio
                };

                var json = JsonConvert.SerializeObject(requestData);
                Console.WriteLine($"JSON запрос: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Очищаем заголовки перед установкой нового токена
                _httpClient.DefaultRequestHeaders.Clear();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // Добавляем дополнительные заголовки
                _httpClient.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

                // Пробуем разные базовые URL
                string[] baseUrls = {
            "https://gigachat.devices.sberbank.ru/api/v1",
            "https://developers.sber.ru/gigachat/api/v1"
        };

                foreach (var baseUrl in baseUrls)
                {
                    try
                    {
                        string apiUrl = $"{baseUrl}/images/generations";
                        Console.WriteLine($"Попытка отправки запроса на: {apiUrl}");

                        var response = await _httpClient.PostAsync(apiUrl, content);

                        Console.WriteLine($"Статус ответа от {apiUrl}: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            var responseJson = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Успешный ответ от {apiUrl}");
                            return JsonConvert.DeserializeObject<ImageGenerationResponse>(responseJson);
                        }
                        else
                        {
                            string error = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Ошибка от {apiUrl}: {response.StatusCode}");
                            Console.WriteLine($"Детали ошибки: {error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Исключение при запросе к {baseUrl}: {ex.Message}");
                    }
                }

                throw new Exception("Все попытки подключения к API завершились неудачей");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение в GenerateImageAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка в GenerateImageAsync: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(imageUrl);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка скачивания изображения: {ex.Message}");
            }
        }
    }

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
}