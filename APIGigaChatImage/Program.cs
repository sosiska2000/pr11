using APIGigaChatImageWPF.Models.Response; // Использование моделей ответов из WPF-приложения
using Newtonsoft.Json; // Использование библиотеки Newtonsoft.Json для работы с JSON
using Newtonsoft.Json.Linq; // Использование LINQ для JSON (JObject) для динамического парсинга JSON
using System; // Использование базовых классов .NET (Console, Exception, DateTime и т.д.)
using System.Collections.Generic; // Использование коллекций (List, Dictionary и т.д.)
using System.IO; // Использование классов для работы с файловой системой
using System.Net.Http; // Использование HttpClient для HTTP-запросов
using System.Text; // Использование классов для работы с кодировкой (Encoding)
using System.Text.RegularExpressions; // Использование регулярных выражений для парсинга HTML
using System.Threading.Tasks; // Использование асинхронного программирования (Task, async/await)

namespace APIGigaChatImage // Основное пространство имен программы
{
    public class Program // Основной класс программы
    {
        // Идентификатор клиента для авторизации в API GigaChat (из второго приложения)
        public static string ClientId = "019b2b50-ff41-77e2-b0f5-23e0ba29e4ef";

        // Ключ авторизации в формате base64 для получения токена доступа
        public static string AutorizationKey = "MDE5YjJiNTAtZmY0MS03N2UyLWIwZjUtMjNlMGJhMjllNGVmOjdkMzViZmFmLTUzNWItNDg0ZS04NDIyLWY0ZmYwNzI2OGIzMg==";

        // Основной асинхронный метод - точка входа в программу
        static async Task Main(string[] args)
        {
            // Установка кодировки вывода консоли в UTF-8 для корректного отображения русских символов
            Console.OutputEncoding = Encoding.UTF8;

            // Вывод заголовка программы
            Console.WriteLine("Генератор изображений GigaChat");
            Console.WriteLine("");

            // Обработка возможных исключений
            try
            {
                // Шаг 1: Получаем токен доступа для API
                Console.WriteLine("Получение токена...");
                string token = await GetToken(ClientId, AutorizationKey);

                // Проверка успешности получения токена
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Ошибка: Не удалось получить токен");
                    Console.ReadKey(); // Ожидание нажатия клавиши перед выходом
                    return; // Завершение программы
                }

                // Вывод информации о полученном токене (первые 30 символов для безопасности)
                Console.WriteLine($"Токен получен: {token.Substring(0, Math.Min(30, token.Length))}...");
                Console.WriteLine(); // Пустая строка для читаемости

                // Шаг 2: Получение описания для генерации изображения от пользователя
                Console.Write("Введите описание для генерации изображения: ");
                string prompt = Console.ReadLine(); // Чтение ввода пользователя

                // Проверка на пустой ввод - использование промпта по умолчанию
                if (string.IsNullOrEmpty(prompt))
                {
                    prompt = "Красивый закат в горах, цифровое искусство";
                    Console.WriteLine($"Используется промпт по умолчанию: {prompt}");
                }

                Console.WriteLine(); // Пустая строка для читаемости
                Console.WriteLine("Генерация изображения...");

                // Шаг 3: Генерация и сохранение изображения
                string imagePath = await GenerateAndSaveImageAsync(token, prompt);

                // Проверка успешности генерации изображения
                if (!string.IsNullOrEmpty(imagePath))
                {
                    Console.WriteLine($"Изображение сгенерировано и сохранено!");
                    Console.WriteLine($"Путь: {imagePath}");

                    // Проверка существования файла и вывод информации о нем
                    if (File.Exists(imagePath))
                    {
                        FileInfo fileInfo = new FileInfo(imagePath);
                        Console.WriteLine($"  Размер файла: {fileInfo.Length} байт ({(fileInfo.Length / 1024):N0} KB)");

                        // Предложение пользователю установить изображение в качестве обоев
                        Console.WriteLine();
                        Console.Write("Хотите установить изображение как обои? (y/n): ");
                        string answer = Console.ReadLine();

                        // Проверка положительного ответа (y/д)
                        if (answer?.ToLower() == "y" || answer?.ToLower() == "д")
                        {
                            try
                            {
                                SetWallpaper(imagePath); // Установка обоев
                                Console.WriteLine("Обои успешно установлены!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка установки обоев: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось создать изображение");
                }
            }
            catch (Exception ex) // Обработка любых непредвиденных исключений
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            // Ожидание нажатия любой клавиши перед завершением программы
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        // Метод для получения OAuth токена доступа к API GigaChat
        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string returnToken = null; // Переменная для хранения полученного токена
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth"; // URL эндпоинта получения токена

            // Создание обработчика HTTP-запросов с настройками
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                // Отключение проверки SSL-сертификатов (только для тестовых серверов, не для продакшена)
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                // Создание HTTP-клиента с использованием настроенного обработчика
                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        // Создание POST-запроса к эндпоинту получения токена
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                        // Добавление необходимых заголовков
                        request.Headers.Add("Accept", "application/json"); // Ожидание JSON-ответа
                        request.Headers.Add("RqUID", rqUID); // Уникальный идентификатор запроса
                        request.Headers.Add("Authorization", $"Bearer {bearer}"); // Авторизационный ключ

                        // Создание данных для отправки в теле запроса
                        var data = new List<KeyValuePair<string, string>>
                        {
                            // Параметр scope с указанием требуемых разрешений
                            new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                        };

                        // Установка данных запроса в формате application/x-www-form-urlencoded
                        request.Content = new FormUrlEncodedContent(data);

                        // Вывод информации о запросе
                        Console.WriteLine($"Отправка запроса на {url}");

                        // Асинхронная отправка HTTP-запроса
                        HttpResponseMessage response = await client.SendAsync(request);

                        // Вывод статуса ответа
                        Console.WriteLine($"Статус ответа: {response.StatusCode}");

                        // Проверка успешности HTTP-запроса
                        if (response.IsSuccessStatusCode)
                        {
                            // Чтение содержимого ответа
                            string responseContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Токен получен успешно");

                            // Десериализация JSON-ответа в объект ResponseToken
                            ResponseToken token = JsonConvert.DeserializeObject<ResponseToken>(responseContent);
                            returnToken = token.access_token; // Извлечение токена доступа
                        }
                        else
                        {
                            // Чтение и вывод деталей ошибки
                            string errorContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Ошибка получения токена: {response.StatusCode}");
                            Console.WriteLine($"Детали ошибки: {errorContent}");
                        }
                    }
                    catch (Exception ex) // Обработка исключений при получении токена
                    {
                        Console.WriteLine($"Исключение при получении токена: {ex.Message}");
                    }
                }
            }
            return returnToken; // Возврат токена (или null в случае ошибки)
        }

        // Основной метод для генерации и сохранения изображения через GigaChat API
        public static async Task<string> GenerateAndSaveImageAsync(string token, string prompt)
        {
            try
            {
                // URL эндпоинта для генерации контента через чат
                string apiUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                // Создание обработчика HTTP-запросов
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    // Отключение проверки SSL (только для тестов)
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                    // Создание HTTP-клиента
                    using (HttpClient client = new HttpClient(handler))
                    {
                        // Настройка заголовков клиента
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        client.DefaultRequestHeaders.Add("X-Client-ID", ClientId);
                        client.Timeout = TimeSpan.FromSeconds(120); // Установка таймаута 2 минуты

                        // Создание объекта запроса с промптом пользователя
                        var requestData = new
                        {
                            model = "GigaChat", // Используемая модель
                            messages = new[]
                            {
                                new { role = "user", content = prompt } // Сообщение пользователя
                            },
                            function_call = "auto" // Автоматический вызов функций
                        };

                        // Сериализация запроса в JSON
                        string json = JsonConvert.SerializeObject(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        // Отправка запроса к API
                        Console.WriteLine($"Отправка запроса в GigaChat...");
                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                        Console.WriteLine($"Статус ответа: {response.StatusCode}");

                        // Проверка успешности ответа
                        if (response.IsSuccessStatusCode)
                        {
                            // Чтение JSON-ответа
                            string responseJson = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Получен ответ от API");

                            // Парсинг JSON для извлечения HTML-контента
                            var data = JObject.Parse(responseJson);
                            string htmlContent = data["choices"]?[0]?["message"]?["content"]?.ToString();

                            // Проверка на пустой ответ
                            if (string.IsNullOrEmpty(htmlContent))
                            {
                                Console.WriteLine("Пустой ответ от нейросети");
                                return null;
                            }

                            // Вывод первых 200 символов HTML для отладки
                            Console.WriteLine($"HTML ответ (первые 200 символов): {htmlContent.Substring(0, Math.Min(200, htmlContent.Length))}");

                            // Поиск URL изображения в HTML с помощью регулярных выражений
                            var match = Regex.Match(htmlContent, @"src=""([^""]+)""");

                            // Альтернативный поиск, если первый не сработал
                            if (!match.Success)
                            {
                                match = Regex.Match(htmlContent, @"<img[^>]+src=['""]([^'""]+)['""]");

                                if (!match.Success)
                                {
                                    Console.WriteLine("Не найдено изображение в ответе");
                                    return null;
                                }
                            }

                            // Извлечение ID изображения из найденного URL
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

                                // Генерация имени файла на основе текущей даты и времени
                                string fileName = $"generated_{DateTime.Now:yyyyMMddHHmmss}.jpg";

                                // Формирование полного пути к файлу
                                string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

                                // Сохранение изображения на диск
                                File.WriteAllBytes(filePath, imageData);
                                Console.WriteLine($"Изображение сохранено: {filePath}");

                                return filePath; // Возврат пути к сохраненному файлу
                            }
                            else
                            {
                                // Обработка ошибки скачивания
                                string error = await fileResponse.Content.ReadAsStringAsync();
                                Console.WriteLine($"Ошибка скачивания изображения: {fileResponse.StatusCode}");
                                Console.WriteLine($"Детали: {error}");
                                return null;
                            }
                        }
                        else
                        {
                            // Обработка ошибки API
                            string error = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Ошибка API: {response.StatusCode}");
                            Console.WriteLine($"Детали: {error}");
                            return null;
                        }
                    }
                }
            }
            catch (TaskCanceledException) // Обработка таймаута
            {
                Console.WriteLine("Превышено время ожидания. Попробуйте упростить запрос.");
                return null;
            }
            catch (Exception ex) // Обработка других исключений
            {
                Console.WriteLine($"Ошибка создания изображения: {ex.Message}");
                return null;
            }
        }

        // Импорт WinAPI функции для установки обоев
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SystemParametersInfo(
            int uAction,      // Действие (установка обоев)
            int uParam,       // Дополнительный параметр (не используется)
            string lpvParam,  // Путь к файлу изображения
            int fuWinIni      // Флаги применения изменений
        );

        // Константы для функции SystemParametersInfo
        private const int SPI_SETDESKWALLPAPER = 20; // Код действия "установить обои"
        private const int SPIF_UPDATEINIFILE = 0x01; // Флаг обновления файла инициализации
        private const int SPIF_SENDWININICHANGE = 0x02; // Флаг отправки уведомления об изменении

        // Метод для установки изображения в качестве обоев рабочего стола
        private static void SetWallpaper(string imagePath)
        {
            try
            {
                // Вызов WinAPI функции для установки обоев
                SystemParametersInfo(
                    SPI_SETDESKWALLPAPER, // Действие: установить обои
                    0, // Неиспользуемый параметр
                    imagePath, // Путь к файлу изображения
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE // Флаги: обновить настройки и уведомить систему
                );
                Console.WriteLine($"Обои установлены: {imagePath}");
            }
            catch (Exception ex) // Обработка исключений при установке обоев
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                throw; // Проброс исключения дальше
            }
        }
    }

    // Класс для десериализации ответа с токеном (дублируется в конце файла)
    public class ResponseToken
    {
        public string access_token { get; set; } // Токен доступа
        public string expires_at { get; set; } // Время истечения срока действия
    }
}