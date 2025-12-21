using System.Collections.Generic;

namespace APIGigaChatImageWPF.Models.Response
{
    public class ChatResponse
    {
        public List<Choice> choices { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public string @object { get; set; }
        public Usage usage { get; set; }

        public class Usage
        {
            public int completion_tokens { get; set; }
            public int prompt_tokens { get; set; }
            public int total_tokens { get; set; }
        }

        public class Choice
        {
            public string finish_reason { get; set; }
            public int index { get; set; }
            public Message message { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}