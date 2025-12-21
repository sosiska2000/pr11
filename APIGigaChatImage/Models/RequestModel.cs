using System.Collections.Generic;

namespace APIGigaChatImageWPF.Models.Response
{
    public class RequestModel
    {
        public string model { get; set; } = "GigaChat";
        public List<Message> messages { get; set; }
        public bool stream { get; set; } = false;
        public int repetition_penalty { get; set; } = 1;

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}