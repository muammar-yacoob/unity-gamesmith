using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SparkGames.UnityAIAgent.Editor
{
    /// <summary>
    /// HTTP client for communicating with AI agent APIs (Ollama, OpenAI, etc.)
    /// </summary>
    public class AIAgentClient
    {
        private readonly AIAgentConfig config;

        public AIAgentClient(AIAgentConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Send a prompt to the AI agent and get a response
        /// </summary>
        public IEnumerator SendPromptAsync(string prompt, Action<string> onSuccess, Action<string> onError)
        {
            var requestData = new
            {
                model = config.model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = config.temperature
                }
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(config.apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(config.apiKey))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                }

                request.timeout = config.timeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseText = request.downloadHandler.text;
                        var response = JsonUtility.FromJson<AIResponse>(responseText);
                        onSuccess?.Invoke(response.response);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse AI response: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"AI Request failed: {request.error}");
                }
            }
        }

        [Serializable]
        private class AIResponse
        {
            public string response;
        }
    }
}
