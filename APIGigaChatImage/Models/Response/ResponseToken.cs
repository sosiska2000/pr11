using Newtonsoft.Json; // Использование библиотеки Newtonsoft.Json для работы с атрибутами сериализации

namespace APIGigaChatImageWPF.Models.Response // Пространство имен для моделей ответов WPF-приложения
{
    // Класс для десериализации ответа от сервера авторизации (OAuth) при запросе токена доступа
    // Этот класс соответствует JSON-ответу от конечной точки получения токена
    public class ResponseToken
    {
        // Токен доступа (access token) для авторизации в API GigaChat
        // Используется атрибут JsonProperty для точного сопоставления с JSON-полем "access_token"
        [JsonProperty("access_token")]
        public string access_token { get; set; }

        // Время истечения срока действия токена в формате Unix timestamp (секунды с 1 января 1970 года)
        // Тип long используется для хранения больших временных значений
        // Используется атрибут JsonProperty для точного сопоставления с JSON-полем "expires_at"
        [JsonProperty("expires_at")]
        public long expires_at { get; set; }
    }
}