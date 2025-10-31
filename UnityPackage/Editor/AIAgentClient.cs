using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple AI client for making API calls using UniTask
    /// </summary>
    public class AIAgentClient
    {
        private readonly GameSmithConfig config;

        public AIAgentClient(GameSmithConfig config)
        {
            this.config = config;
        }

        public async UniTask<string> SendMessageAsync(string userMessage, string systemContext, CancellationToken cancellationToken = default)
        {
            if (!config.IsValid())
            {
                throw new Exception(config.GetValidationMessage());
            }

            // Build request body (OpenAI format - compatible with most APIs)
            var requestBody = $@"{{
                ""model"": ""{config.GetCurrentModel()}"",
                ""messages"": [
                    {{""role"": ""system"", ""content"": ""{EscapeJson(systemContext)}""}},
                    {{""role"": ""user"", ""content"": ""{EscapeJson(userMessage)}""}}
                ],
                ""temperature"": {config.temperature},
                ""max_tokens"": {config.maxTokens}
            }}";

            using (var request = new UnityWebRequest(config.apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(config.apiKey))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                }

                request.timeout = 120;

                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    var content = ParseResponse(responseText);

                    if (!string.IsNullOrEmpty(content))
                    {
                        return content;
                    }
                    else
                    {
                        throw new Exception("No response from AI");
                    }
                }
                else
                {
                    throw new Exception($"Request failed: {request.error}\n{request.downloadHandler.text}");
                }
            }
        }

        private string ParseResponse(string json)
        {
            // Simple JSON parsing for the response content
            // Looking for: "choices":[{"message":{"content":"..."}}]

            var contentStart = json.IndexOf("\"content\"", StringComparison.Ordinal);
            if (contentStart == -1) return null;

            contentStart = json.IndexOf(":", contentStart) + 1;
            contentStart = json.IndexOf("\"", contentStart) + 1;

            var contentEnd = json.IndexOf("\"", contentStart);
            if (contentEnd == -1) return null;

            return json.Substring(contentStart, contentEnd - contentStart)
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
