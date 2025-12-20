// В WPF проекте: Services/ApiService.cs
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using APIGigaChatImageWPF.Models.Response;

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
            string clientId = ConfigurationManager.AppSettings["ClientId"];
            string authKey = ConfigurationManager.AppSettings["AuthorizationKey"];
            string url = ConfigurationManager.AppSettings["AuthUrl"];

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

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<ResponseToken>(responseContent);

                _accessToken = tokenResponse.access_token;
                return _accessToken;
            }

            throw new Exception("Не удалось получить токен");
        }

        public async Task<ImageGenerationResponse> GenerateImageAsync(
            string prompt,
            string style = "realistic",
            string colorPalette = "vibrant",
            string aspectRatio = "16:9")
        {
            if (string.IsNullOrEmpty(_accessToken))
                await GetTokenAsync();

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
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.PostAsync(
                $"{ConfigurationManager.AppSettings["ApiBaseUrl"]}/images/generations",
                content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ImageGenerationResponse>(responseJson);
            }

            throw new Exception($"Ошибка генерации: {response.StatusCode}");
        }

        public async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            return await _httpClient.GetByteArrayAsync(imageUrl);
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