using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Cod.Platform.OpenAI
{
    internal sealed class OpenAIService(
        IOptions<OpenAIServiceOptions> options, 
        HttpClient httpClient, 
        ILogger<OpenAIService> logger) 
        : IOpenAIService
    {
        private static readonly JsonSerializerOptions SERIALIZATION_OPTIONS = new(JsonSerializerDefaults.Web);
        private const string JSON_CONTENT_TYPE = "application/json";

        public async Task<OpenAIConversationAnalysisResult?> AnalyzeSOAPAsync(string id, string conversation, CancellationToken cancellationToken = default)
        {
            var userInput = new List<object>();
            var lines = conversation.Split('\n');
            foreach (var line in lines)
            {
                userInput.Add(new
                {
                    type = "text",
                    text = line
                });
            }

            var payload = new
            {
                messages = new object[]
                {
                      new {
                          role = "system",
                          content = new object[] {
                              new {
                                  type = "text",
                                  text = options.Value.SystemPrompt,
                              }
                          }
                      },
                      new {
                          role = "user",
                          content = userInput
                      }
                },
                temperature = 0.1,
                top_p = 0.95,
                max_tokens = 4096,
                stream = false
            };

            var json = Serialize(payload);

            using var response = await httpClient.PostAsync("chat/completions?api-version=2024-02-15-preview", new StringContent(json, Encoding.UTF8, JSON_CONTENT_TYPE), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Error getting response from OpenAI: {response.StatusCode}, {response.ReasonPhrase} on message {id}.");
                return null;
            }

            var respbody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = Deserialize<OpenAIConversationAnalysisResult>(respbody);
            if (result == null)
            {
                logger.LogError($"Error deserializing OpenAI response: {respbody} on message {id}.");
                return null;
            }

            return result;
        }

        private static string Serialize(object obj) => System.Text.Json.JsonSerializer.Serialize(obj, SERIALIZATION_OPTIONS);
        private static T Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, SERIALIZATION_OPTIONS)!;
    }
}
