using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage.Models.Response
{
    public class ResponseToken
    {
        [JsonProperty("access_token")]
        public string access_token { get; set; }

        [JsonProperty("expires_at")]
        public long expires_at { get; set; }
    }
}
