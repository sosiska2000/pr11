using APIGigaChatImageWPF.Models.Response; // Использование моделей ответов из WPF-приложения
using Newtonsoft.Json; // Использование библиотеки Newtonsoft.Json для работы с JSON
using Newtonsoft.Json.Linq; // Использование LINQ для JSON (JObject) для динамического парсинга JSON
using System; // Использование базовых классов .NET (Console, Exception, DateTime, Environment и т.д.)
using System.Collections.Generic; // Использование коллекций (List, KeyValuePair и т.д.)
using System.Configuration; // Использование конфигурации приложения (хотя в коде не используется явно)
using System.Net.Http; // Использование HttpClient для HTTP-запросов
using System.Text; // Использование классов для работы с кодировкой (Encoding)
using System.Text.RegularExpressions; // Использование регулярных выражений для парсинга HTML
using System.Threading.Tasks; // Использование асинхронного программирования (Task, async/await)

namespace APIGigaChatImageWPF.Services // Пространство имен для сервисных классов WPF-приложения
{
    // Класс сервиса для работы с API GigaChat (генерация изображений)
    // Предоставляет методы для получения токена, генерации и сохранения изображений
    public class ApiService
    {
        // Поля класса
        private HttpClient _httpClient; // HTTP-клиент для выполнения запросов
        private string _accessToken; // Токен доступа для авторизации в API
        private readonly string _clientId; // Идентификатор клиента (только для чтения)
        private readonly string _authKey; // Ключ авторизации (только для чтения)

        // Конструктор класса - инициализирует сервис
        public ApiService()
        {
            // Используем рабочие ключи из второго приложения
            // Эти ключи используются для получения OAuth токена
            _clientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";
            _authKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOjdkMzViZmFmLTUzNWItNDg0ZS04NDIyLWY0ZmYwNzI2OGIzMg==";

            // Инициализация HTTP-клиента с кастомным обработчиком
            _httpClient = new HttpClient(new HttpClientHandler
            {
                // Отключение проверки SSL-сертификатов (для тестовых окружений)
                // ВНИМАНИЕ: Не использовать в продакшене!
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
        }

        // Метод для получения OAuth токена доступа
        public async Task<string> GetTokenAsync()
        {
            try
            {
                // URL эндпоинта для получения токена
                string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

                // Вывод отладочной информации
                Console.WriteLine($"Получение токена с ClientId: {_clientId}");
                Console.WriteLine($"URL: {url}");

                // Создание POST-запроса
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                // Добавление необходимых заголовков
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("RqUID", _clientId);
                request.Headers.Add("Authorization", $"Bearer {_authKey}");

                // Подготовка данных для отправки
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS") // Запрашиваемые разрешения
                };

                // Установка данных в теле запроса
                request.Content = new FormUrlEncodedContent(data);

                // Отправка запроса
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                Console.WriteLine($"Статус ответа при получении токена: {response.StatusCode}");

                // Проверка успешности запроса
                if (response.IsSuccessStatusCode)
                {
                    // Чтение и десериализация ответа
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Токен успешно получен");

                    var tokenResponse = JsonConvert.DeserializeObject<ResponseToken>(responseContent);

                    // Сохранение токена в поле класса
                    _accessToken = tokenResponse.access_token;
                    Console.WriteLine($"Токен получен (длина: {_accessToken?.Length ?? 0} символов)");
                    return _accessToken; // Возврат токена
                }
                else
                {
                    // Обработка ошибки
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка получения токена: {response.StatusCode}");
                    Console.WriteLine($"Детали: {error}");
                    throw new Exception($"Ошибка получения токена: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                // Детальное логирование исключения
                Console.WriteLine($"Исключение в GetTokenAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка в GetTokenAsync: {ex.Message}", ex);
            }
        }

        // Основной метод для генерации и сохранения изображения
        public async Task<string> GenerateAndSaveImageAsync(string prompt)
        {
            try
            {
                // Проверка наличия токена, при необходимости получение нового
                if (string.IsNullOrEmpty(_accessToken))
                    await GetTokenAsync();

                // URL API для генерации контента
                string apiUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                // Создание нового HTTP-клиента для этого запроса
                using (var client = new HttpClient(new HttpClientHandler
                {
                    // Отключение проверки SSL
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                }))
                {
                    // Настройка заголовков запроса
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                    client.DefaultRequestHeaders.Add("X-Client-ID", _clientId);
                    client.Timeout = TimeSpan.FromSeconds(120); // Таймаут 2 минуты

                    // Подготовка данных запроса
                    var requestData = new
                    {
                        model = "GigaChat", // Используемая модель
                        messages = new[] // Массив сообщений
                        {
                            new { role = "user", content = prompt } // Сообщение пользователя с промптом
                        },
                        function_call = "auto" // Автоматический вызов функций
                    };

                    // Сериализация в JSON и создание контента
                    string json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    Console.WriteLine($"Отправка запроса в GigaChat...");

                    // Отправка POST-запроса
                    var response = await client.PostAsync(apiUrl, content);

                    // Обработка успешного ответа
                    if (response.IsSuccessStatusCode)
                    {
                        // Чтение ответа
                        string responseJson = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Получен ответ от API");

                        // Парсинг JSON для извлечения HTML-контента
                        var data = JObject.Parse(responseJson);
                        string htmlContent = data["choices"]?[0]?["message"]?["content"]?.ToString();

                        // Проверка на пустой ответ
                        if (string.IsNullOrEmpty(htmlContent))
                        {
                            Console.WriteLine("Пустой ответ от нейросети");
                            throw new Exception("Пустой ответ от нейросети");
                        }

                        // Вывод отладочной информации (первые 200 символов)
                        Console.WriteLine($"HTML ответ (первые 200 символов): {htmlContent.Substring(0, Math.Min(200, htmlContent.Length))}");

                        // Извлечение URL изображения из HTML с помощью регулярных выражений
                        var match = Regex.Match(htmlContent, @"src=""([^""]+)""");

                        // Альтернативный поиск, если первый не сработал
                        if (!match.Success)
                        {
                            match = Regex.Match(htmlContent, @"<img[^>]+src=['""]([^'""]+)['""]");

                            if (!match.Success)
                            {
                                Console.WriteLine($"Не найдено изображение в ответе");
                                throw new Exception("Не найдено изображение в ответе");
                            }
                        }

                        // Извлечение ID изображения
                        string imageId = match.Groups[1].Value;
                        Console.WriteLine($"ID изображения получен: {imageId}");

                        // Формирование URL для скачивания изображения
                        string fileUrl = $"https://gigachat.devices.sberbank.ru/api/v1/files/{imageId}/content";
                        Console.WriteLine($"URL для скачивания: {fileUrl}");

                        // Скачивание изображения
                        Console.WriteLine($"Скачивание изображения...");
                        var fileResponse = await client.GetAsync(fileUrl);

                        if (fileResponse.IsSuccessStatusCode)
                        {
                            // Чтение изображения как массива байтов
                            byte[] imageData = await fileResponse.Content.ReadAsByteArrayAsync();
                            Console.WriteLine($"Изображение скачано: {imageData.Length} байт");

                            // Определение пути для сохранения в папке Pictures
                            string picturesPath = System.IO.Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                                "GigaChat Wallpapers");

                            // Создание директории, если она не существует
                            if (!System.IO.Directory.Exists(picturesPath))
                                System.IO.Directory.CreateDirectory(picturesPath);

                            // Генерация имени файла на основе даты и времени
                            string fileName = $"wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                            string filePath = System.IO.Path.Combine(picturesPath, fileName);

                            // Сохранение изображения на диск
                            System.IO.File.WriteAllBytes(filePath, imageData);
                            Console.WriteLine($"Изображение сохранено: {filePath}");

                            return filePath; // Возврат пути к сохраненному файлу
                        }
                        else
                        {
                            // Обработка ошибки скачивания
                            string error = await fileResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"Ошибка скачивания изображения: {fileResponse.StatusCode}");
                            Console.WriteLine($"Детали: {error}");
                            throw new Exception($"Ошибка скачивания изображения: {fileResponse.StatusCode}");
                        }
                    }
                    else
                    {
                        // Обработка ошибки API
                        string error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ошибка API: {response.StatusCode}");
                        Console.WriteLine($"Детали: {error}");
                        throw new Exception($"Ошибка генерации: {response.StatusCode} - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Детальное логирование исключения
                Console.WriteLine($"Ошибка в GenerateAndSaveImageAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка генерации: {ex.Message}", ex);
            }
        }

        // Упрощенный метод для ручного режима с дополнительными параметрами
        public async Task<string> GenerateImageAsync(
            string prompt,
            string style = "реализм",       // Стиль изображения (значение по умолчанию)
            string colorPalette = "яркая",  // Цветовая палитра (значение по умолчанию)
            string aspectRatio = "16:9")    // Соотношение сторон (значение по умолчанию)
        {
            // Строим улучшенный промпт с учетом всех параметров
            string enhancedPrompt = $"{prompt}, стиль: {style}, цветовая палитра: {colorPalette}, " +
                                   $"соотношение сторон: {aspectRatio}, высокое качество, детализированное, " +
                                   "профессиональная графика, обои рабочего стола";

            // Вызов основного метода с улучшенным промптом
            return await GenerateAndSaveImageAsync(enhancedPrompt);
        }
    }
}