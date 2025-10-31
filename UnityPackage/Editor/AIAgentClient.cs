using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// AI client with MCP tool support
    /// </summary>
    public class AIAgentClient
    {
        private readonly GameSmithConfig config;
        private List<object> conversationHistory = new List<object>();
        private string lastSystemContext = "";

        public AIAgentClient(GameSmithConfig config)
        {
            this.config = config;
        }

        public void SendMessage(string userMessage, string systemContext, List<MCPTool> tools,
            Action<AIResponse> onSuccess, Action<string> onError)
        {
            if (!config.IsValid())
            {
                onError?.Invoke(config.GetValidationMessage());
                return;
            }

            // Add user message to history
            conversationHistory.Add(new Dictionary<string, object>
            {
                { "role", "user" },
                { "content", userMessage }
            });

            lastSystemContext = systemContext;
            var coroutine = SendWebRequest(systemContext, tools, onSuccess, onError);
            EditorCoroutineRunner.StartCoroutine(coroutine);
        }

        public void SendToolResult(string toolUseId, string toolResult, string systemContext, List<MCPTool> tools,
            Action<AIResponse> onSuccess, Action<string> onError)
        {
            // Add tool result to history
            conversationHistory.Add(new Dictionary<string, object>
            {
                { "role", "user" },
                { "content", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "tool_result" },
                            { "tool_use_id", toolUseId },
                            { "content", toolResult }
                        }
                    }
                }
            });

            var coroutine = SendWebRequest(systemContext, tools, onSuccess, onError);
            EditorCoroutineRunner.StartCoroutine(coroutine);
        }

        private IEnumerator SendWebRequest(string systemContext, List<MCPTool> tools,
            Action<AIResponse> onSuccess, Action<string> onError)
        {
            // Build Claude API request
            var requestDict = new Dictionary<string, object>
            {
                { "model", config.GetCurrentModel() },
                { "max_tokens", config.maxTokens },
                { "temperature", config.temperature },
                { "system", systemContext },
                { "messages", conversationHistory }
            };

            // Add tools if available
            if (tools != null && tools.Count > 0)
            {
                var toolsArray = new List<object>();
                foreach (var tool in tools)
                {
                    toolsArray.Add(new Dictionary<string, object>
                    {
                        { "name", tool.Name },
                        { "description", tool.Description },
                        { "input_schema", tool.InputSchema ?? new Dictionary<string, object>() }
                    });
                }
                requestDict["tools"] = toolsArray;
            }

            var requestBody = MiniJSON.Json.Serialize(requestDict);

            using (var request = new UnityWebRequest(config.apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("anthropic-version", "2023-06-01");
                request.SetRequestHeader("x-api-key", config.apiKey);

                request.timeout = 120;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    var response = ParseClaudeResponse(responseText);

                    if (response != null)
                    {
                        // Add assistant response to history
                        conversationHistory.Add(new Dictionary<string, object>
                        {
                            { "role", "assistant" },
                            { "content", response.ContentBlocks }
                        });

                        onSuccess?.Invoke(response);
                    }
                    else
                    {
                        onError?.Invoke("Failed to parse AI response");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}\n{request.downloadHandler.text}");
                }
            }
        }

        private AIResponse ParseClaudeResponse(string json)
        {
            try
            {
                var response = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                if (response == null) return null;

                var result = new AIResponse
                {
                    ContentBlocks = new List<object>()
                };

                if (response.ContainsKey("content"))
                {
                    var content = response["content"] as List<object>;
                    if (content != null)
                    {
                        foreach (var item in content)
                        {
                            var block = item as Dictionary<string, object>;
                            if (block != null)
                            {
                                result.ContentBlocks.Add(block);

                                var type = block.ContainsKey("type") ? block["type"].ToString() : "";

                                if (type == "text" && block.ContainsKey("text"))
                                {
                                    result.TextContent += block["text"].ToString();
                                }
                                else if (type == "tool_use")
                                {
                                    result.HasToolUse = true;
                                    result.ToolUseId = block.ContainsKey("id") ? block["id"].ToString() : "";
                                    result.ToolName = block.ContainsKey("name") ? block["name"].ToString() : "";
                                    result.ToolInput = block.ContainsKey("input") ? block["input"] as Dictionary<string, object> : new Dictionary<string, object>();
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Failed to parse response: {ex.Message}");
                return null;
            }
        }

        public void ClearHistory()
        {
            conversationHistory.Clear();
        }
    }

    /// <summary>
    /// Represents an AI response with potential tool uses
    /// </summary>
    public class AIResponse
    {
        public string TextContent = "";
        public List<object> ContentBlocks = new List<object>();
        public bool HasToolUse = false;
        public string ToolUseId = "";
        public string ToolName = "";
        public Dictionary<string, object> ToolInput = new Dictionary<string, object>();
    }
}
