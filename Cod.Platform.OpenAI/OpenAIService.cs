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

        public async Task<OpenAIConversationAnalysisResult?> AnalyzeSOAPAsync(string id, int kind, string conversation, string? outputLanguage, CancellationToken cancellationToken = default)
        {
            if (!options.Value.SystemPrompts.TryGetValue(kind, out string? systemPrompt))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            if (outputLanguage != null)
            {
                systemPrompt = $"{systemPrompt} The output language should in {outputLanguage}.";
            }

            List<object> userInput = [];
            string[] lines = conversation.Split('\n');
            foreach (string line in lines)
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
                                  text = systemPrompt,
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

            string json = Serialize(payload);

            using HttpResponseMessage response = await httpClient.PostAsync("chat/completions?api-version=2024-02-15-preview", new StringContent(json, Encoding.UTF8, JSON_CONTENT_TYPE), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Error getting response from OpenAI: {response.StatusCode}, {response.ReasonPhrase} on message {id}.");
                return null;
            }

            string respbody = await response.Content.ReadAsStringAsync(cancellationToken);
            OpenAIConversationAnalysisResult result = Deserialize<OpenAIConversationAnalysisResult>(respbody);
            if (result == null)
            {
                logger.LogError($"Error deserializing OpenAI response: {respbody} on message {id}.");
                return null;
            }

            return result;
        }

        private static string Serialize(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, SERIALIZATION_OPTIONS);
        }

        private static T Deserialize<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, SERIALIZATION_OPTIONS)!;
        }
    }
}
