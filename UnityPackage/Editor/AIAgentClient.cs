using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple AI client for making API calls
    /// </summary>
    public class AIAgentClient
    {
        private readonly GameSmithConfig config;

        public AIAgentClient(GameSmithConfig config)
        {
            this.config = config;
        }

        public IEnumerator SendMessageAsync(string userMessage, string systemContext, Action<string> onSuccess, Action<string> onError)
        {
            if (!config.IsValid())
            {
                onError?.Invoke(config.GetValidationMessage());
                yield break;
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

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var responseText = request.downloadHandler.text;
                        var content = ParseResponse(responseText);

                        if (!string.IsNullOrEmpty(content))
                        {
                            onSuccess?.Invoke(content);
                        }
                        else
                        {
                            onError?.Invoke("No response from AI");
                        }
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse response: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}\n{request.downloadHandler.text}");
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
