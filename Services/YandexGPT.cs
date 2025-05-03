using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ScanDocumentsPriemDev.Classes
{
    public class YandexGPT
    {
        private static string URL = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";
        private static string KEY = "Api-Key AQVN1fjQej3FWnC9ZYeBuiILAu1dkfPk6pIWb4xP";

        public static Param SetParams() {
            return new Param()
            {
                modelUri = "gpt://b1gg3d4th4a1af0796ro/yandexgpt",
                completionOptions = new Param.CompletionOptions()
                {
                    stream = false,
                    temperature = 0.7f,
                    maxTokens = "7000"
                },
                messages = new List<Param.Message>()
            };
        }

        public static async Task<string> Communication(Param Params) {

            using (HttpClient Client = new HttpClient()) {
                using (var Request = new HttpRequestMessage(HttpMethod.Post, URL)) {
                    Request.Headers.Add("Authorization", KEY);
                    var Content = new StringContent(
                        JsonConvert.SerializeObject(Params), null, "application/json");

                    Request.Content = Content;

                    using (var ClientResponse = await Client.SendAsync(Request)) {
                        string TextBody = await ClientResponse.Content.ReadAsStringAsync();

                        Response Response = JsonConvert.DeserializeObject<Response>(TextBody);
                        string AnswerGPT = Response.result.alternatives[0].message.text;

                        return AnswerGPT;
                    }
                }
            }
        }

        public class Param {
            public string modelUri { get; set; }
            public CompletionOptions completionOptions { get; set; }
            public List<Message> messages { get; set; }

            public class CompletionOptions
            {
                public bool stream { get; set; }
                public double temperature { get; set; }
                public string maxTokens { get; set; }
            }
            public class Message
            {
                public string role { get; set; }
                public string text { get; set; }

                public Message(string text = "") {
                    this.text = text;
                    this.role = "user";
                }
            }
        }
        private class Response {
            public Result result { get; set; }
            public class Result
            {
                public List<Alternative> alternatives { get; set; }
            }

            public class Alternative
            {
                public Message message { get; set; }
            }

            public class Message
            {
                public string role { get; set; }
                public string text { get; set; }
            }
        }
        public static List<string> SplitText(string text, int maxLength = 5000)
        {
            var chunks = new List<string>();
            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                int chunkLength = Math.Min(maxLength, text.Length - currentIndex);
                chunks.Add(text.Substring(currentIndex, chunkLength));
                currentIndex += chunkLength;
            }

            return chunks;
        }
    }
}
