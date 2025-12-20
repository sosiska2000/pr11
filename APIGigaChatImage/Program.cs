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
        public static string AutorizationKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOmZiYjEwNTdmLWM2ZmUtNDAwYS04NThjLTNlMTA2NjRmYTVkMA==";

        static async Task Main(string[] args)
        {
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

                Console.WriteLine($"Токен получен: {token.Substring(0, 30)}...");
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
                    Console.WriteLine($"Изображение сгенерировано! ID: {imageId}");

                    // Дополнительно: скачиваем изображение
                    Console.WriteLine("Хотите скачать изображение? (y/n)");
                    string answer = Console.ReadLine();

                    if (answer?.ToLower() == "y")
                    {
                        Console.WriteLine("Скачивание изображения...");
                        bool downloaded = await DownloadImageAsync(token, imageId, "generated_image.jpg");

                        if (downloaded)
                        {
                            Console.WriteLine("Изображение успешно скачано в файл generated_image.jpg");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка при генерации изображения");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        // Метод для получения токена (уже есть)
        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string returnToken = null;
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, sert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
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

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        ResponseToken token = JsonConvert.DeserializeObject<ResponseToken>(responseContent);

                        returnToken = token.access_token;
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка получения токена: {response.StatusCode}");
                    }
                }
            }
            return returnToken;
        }

        // Шаг 9: Метод для генерации изображения (принимает токен и промпт)
        public static async Task<string> GenerateImageAsync(string token, string prompt)
        {
            string imageId = null;
            string url = "https://gigachat.devices.sberbank.ru/api/v1/images/generations";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, sert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    // Подготавливаем запрос
                    var requestData = new
                    {
                        model = "GigaChat:latest",
                        prompt = prompt,
                        n = 1,
                        size = "1024x1024"
                    };

                    string json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Добавляем заголовок с токеном
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    Console.WriteLine($"Отправка запроса на генерацию с промптом: {prompt}");

                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ответ API: {responseContent}");

                        // Десериализуем ответ
                        var imageResponse = JsonConvert.DeserializeObject<ImageGenerationResponse>(responseContent);

                        if (imageResponse?.data?.Count > 0)
                        {
                            imageId = imageResponse.data[0].id;
                            Console.WriteLine($"URL изображения: {imageResponse.data[0].url}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка генерации изображения: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ошибка: {errorContent}");
                    }
                }
            }

            return imageId;
        }

        // Дополнительный метод для скачивания изображения (по заданию)
        public static async Task<bool> DownloadImageAsync(string token, string imageId, string filePath)
        {
            try
            {
                // Сначала получаем информацию об изображении
                string infoUrl = $"https://gigachat.devices.sberbank.ru/api/v1/images/{imageId}";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, sert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                        // Получаем информацию об изображении
                        HttpResponseMessage infoResponse = await client.GetAsync(infoUrl);

                        if (infoResponse.IsSuccessStatusCode)
                        {
                            string infoContent = await infoResponse.Content.ReadAsStringAsync();
                            var imageInfo = JsonConvert.DeserializeObject<ImageInfoResponse>(infoContent);

                            if (!string.IsNullOrEmpty(imageInfo?.url))
                            {
                                // Скачиваем изображение по URL
                                byte[] imageData = await client.GetByteArrayAsync(imageInfo.url);

                                // Сохраняем в файл
                                File.WriteAllBytes(filePath, imageData);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при скачивании изображения: {ex.Message}");
            }

            return false;
        }
    }
}