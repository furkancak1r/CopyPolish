using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CopyPolish
{
    internal static class OpenRouterClient
    {
        private static readonly Uri Endpoint = new Uri("https://openrouter.ai/api/v1/chat/completions");

        public static string Complete(string apiKey, string model, string systemPrompt, string userContent, string referer = null, string title = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenRouter API anahtarı boş.");

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                if (!string.IsNullOrWhiteSpace(referer))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", referer);
                }
                if (!string.IsNullOrWhiteSpace(title))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", title);
                }

                var payload = new ChatCompletionRequest
                {
                    model = model,
                    messages = new[]
                    {
                        new ChatMessage { role = "system", content = systemPrompt },
                        new ChatMessage { role = "user", content = userContent }
                    }
                };

                var json = Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using (var resp = client.PostAsync(Endpoint, content).Result)
                {
                    var body = resp.Content.ReadAsStringAsync().Result;
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new Exception($"OpenRouter hata: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
                    }

                    var result = Deserialize<ChatCompletionResponse>(body);
                    if (result == null || result.choices == null || result.choices.Length == 0 || result.choices[0].message == null)
                    {
                        throw new Exception("Geçersiz OpenRouter yanıtı.");
                    }
                    return (result.choices[0].message.content ?? string.Empty).Trim();
                }
            }
        }

        public static string CompleteWithFallback(string apiKey, IEnumerable<string> models, string systemPrompt, string userContent, string referer = null, string title = null)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            var errors = new List<string>();
            foreach (var modelName in models)
            {
                var trimmed = modelName?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                try
                {
                    return Complete(apiKey, trimmed, systemPrompt, userContent, referer, title);
                }
                catch (Exception ex)
                {
                    errors.Add($"{trimmed}: {ex.Message}");
                }
            }

            if (errors.Count == 0)
            {
                throw new Exception("Denenecek model bulunamadi.");
            }

            throw new Exception("Tum modeller basarisiz oldu:\n" + string.Join("\n", errors));
        }

        private static string Serialize<T>(T obj)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static T Deserialize<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (T)serializer.ReadObject(ms);
            }
        }

        [DataContract]
        private class ChatCompletionRequest
        {
            [DataMember]
            public string model { get; set; }

            [DataMember]
            public ChatMessage[] messages { get; set; }
        }

        [DataContract]
        private class ChatMessage
        {
            [DataMember]
            public string role { get; set; }

            [DataMember]
            public string content { get; set; }
        }

        [DataContract]
        private class ChatCompletionResponse
        {
            [DataMember]
            public Choice[] choices { get; set; }
        }

        [DataContract]
        private class Choice
        {
            [DataMember]
            public Message message { get; set; }
        }

        [DataContract]
        private class Message
        {
            [DataMember]
            public string role { get; set; }

            [DataMember]
            public string content { get; set; }
        }
    }
}




