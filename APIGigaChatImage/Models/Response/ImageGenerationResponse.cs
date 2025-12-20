using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage.Models.Response
{
    // Классы для десериализации ответов API
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
