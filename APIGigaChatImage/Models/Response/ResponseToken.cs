using Newtonsoft.Json;

namespace APIGigaChatImageWPF.Models.Response
{
    public class ResponseToken
    {
        [JsonProperty("access_token")]
        public string access_token { get; set; }

        [JsonProperty("expires_at")]
        public long expires_at { get; set; }
    }
}