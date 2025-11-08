using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private string lastToolName = "";

        // Public property to access the active provider
        public string ActiveProvider => config?.activeProvider ?? "";

        public AIAgentClient(GameSmithConfig config)
        {
            this.config = config;
        }

        // Helper to decode Unicode escape sequences (u003c -> <, u003e -> >, etc.)
        private string DecodeUnicodeEscapes(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Replace common Unicode escapes
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\u003c", "<");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\u003e", ">");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\u0026", "&");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\u0027", "'");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\u0022", "\"");

            return text;
        }

        public void SendMessage(string userMessage, string systemContext, List<MCPTool> tools,
            Action<AIResponse> onSuccess, Action<string> onError)
        {
            if (!config.IsValid())
            {
                string validationError = config.GetValidationMessage();
                GameSmithLogger.LogWarning($"Config validation failed: {validationError}");
                onError?.Invoke(validationError);
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
            // Add tool result to history based on provider
            if (config.activeProvider.Contains("OpenAI"))
            {
                // OpenAI format: tool message with tool_call_id
                conversationHistory.Add(new Dictionary<string, object>
                {
                    { "role", "tool" },
                    { "tool_call_id", toolUseId },
                    { "content", toolResult }
                });
            }
            else
            {
                // Claude format: user message with tool_result
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
            }

            var coroutine = SendWebRequest(systemContext, tools, onSuccess, onError);
            EditorCoroutineRunner.StartCoroutine(coroutine);
        }

        private IEnumerator SendWebRequest(string systemContext, List<MCPTool> tools,
            Action<AIResponse> onSuccess, Action<string> onError)
        {
            bool isOllamaOrOpenAI = config.activeProvider.Contains("Ollama") || config.activeProvider.Contains("OpenAI") || config.activeProvider.Contains("Grok");
            bool isGemini = config.activeProvider.Contains("Gemini");

            // For Ollama on localhost, normalize the URL

            // Build request based on provider type
            var requestDict = new Dictionary<string, object>();

            if (isGemini)
            {
                // Gemini format
                var contents = new List<object>();

                // Add system instruction as first user message
                if (!string.IsNullOrEmpty(systemContext))
                {
                    contents.Add(new Dictionary<string, object>
                    {
                        { "role", "user" },
                        { "parts", new List<object>
                            {
                                new Dictionary<string, object> { { "text", systemContext } }
                            }
                        }
                    });
                    contents.Add(new Dictionary<string, object>
                    {
                        { "role", "model" },
                        { "parts", new List<object>
                            {
                                new Dictionary<string, object> { { "text", "Understood. I'll help you with Unity development." } }
                            }
                        }
                    });
                }

                // Convert conversation history to Gemini format
                foreach (var msg in conversationHistory)
                {
                    var msgDict = msg as Dictionary<string, object>;
                    if (msgDict != null && msgDict.ContainsKey("role") && msgDict.ContainsKey("content"))
                    {
                        string role = msgDict["role"].ToString();
                        var content = msgDict["content"];

                        // Check if content is a list (parts format - can be tool results or function calls)
                        if (content is List<object> contentList)
                        {
                            var parts = new List<object>();
                            foreach (var item in contentList)
                            {
                                var itemDict = item as Dictionary<string, object>;
                                if (itemDict != null)
                                {
                                    // Check for tool_result (Claude format) - convert to functionResponse
                                    if (itemDict.ContainsKey("type") && itemDict["type"].ToString() == "tool_result")
                                    {
                                        // Need to get function name from somewhere - use the last tool name
                                        parts.Add(new Dictionary<string, object>
                                        {
                                            { "functionResponse", new Dictionary<string, object>
                                                {
                                                    { "name", lastToolName ?? "unknown" },
                                                    { "response", new Dictionary<string, object>
                                                        {
                                                            { "result", itemDict.ContainsKey("content") ? itemDict["content"].ToString() : "" }
                                                        }
                                                    }
                                                }
                                            }
                                        });
                                    }
                                    // Already in Gemini parts format (text, functionCall, etc.) - use directly
                                    else
                                    {
                                        parts.Add(itemDict);
                                    }
                                }
                            }
                            if (parts.Count > 0)
                            {
                                contents.Add(new Dictionary<string, object>
                                {
                                    { "role", role == "assistant" ? "model" : "user" },
                                    { "parts", parts }
                                });
                            }
                        }
                        else
                        {
                            // Simple text content
                            string contentText = content.ToString();
                            contents.Add(new Dictionary<string, object>
                            {
                                { "role", role == "assistant" ? "model" : "user" },
                                { "parts", new List<object>
                                    {
                                        new Dictionary<string, object> { { "text", contentText } }
                                    }
                                }
                            });
                        }
                    }
                }

                requestDict["contents"] = contents;
                requestDict["generationConfig"] = new Dictionary<string, object>
                {
                    { "temperature", config.temperature },
                    { "maxOutputTokens", config.maxTokens }
                };

                // Add tool config for Gemini to enable function calling
                if (tools != null && tools.Count > 0)
                {
                    requestDict["toolConfig"] = new Dictionary<string, object>
                    {
                        { "functionCallingConfig", new Dictionary<string, object>
                            {
                                { "mode", "AUTO" }
                            }
                        }
                    };
                }
            }
            else if (isOllamaOrOpenAI)
            {
                // OpenAI/Ollama format
                requestDict["model"] = config.GetCurrentModel();
                requestDict["max_tokens"] = config.maxTokens;
                requestDict["temperature"] = config.temperature;

                // Add system message to conversation for OpenAI format
                var messagesWithSystem = new List<object>();
                messagesWithSystem.Add(new Dictionary<string, object>
                {
                    { "role", "system" },
                    { "content", systemContext }
                });
                messagesWithSystem.AddRange(conversationHistory);
                requestDict["messages"] = messagesWithSystem;
            }
            else
            {
                // Claude format
                requestDict["model"] = config.GetCurrentModel();
                requestDict["max_tokens"] = config.maxTokens;
                requestDict["temperature"] = config.temperature;
                requestDict["system"] = systemContext;
                requestDict["messages"] = conversationHistory;
            }

            // Add tools if available
            if (tools != null && tools.Count > 0)
            {
                var toolsArray = new List<object>();
                foreach (var tool in tools)
                {
                    if (isGemini)
                    {
                        // Gemini format for tools (function declarations)
                        toolsArray.Add(new Dictionary<string, object>
                        {
                            { "name", tool.Name },
                            { "description", tool.Description },
                            { "parameters", tool.InputSchema ?? new Dictionary<string, object>() }
                        });
                    }
                    else if (isOllamaOrOpenAI)
                    {
                        // OpenAI format for tools
                        toolsArray.Add(new Dictionary<string, object>
                        {
                            { "type", "function" },
                            { "function", new Dictionary<string, object>
                                {
                                    { "name", tool.Name },
                                    { "description", tool.Description },
                                    { "parameters", tool.InputSchema ?? new Dictionary<string, object>() }
                                }
                            }
                        });
                    }
                    else
                    {
                        // Claude format for tools
                        toolsArray.Add(new Dictionary<string, object>
                        {
                            { "name", tool.Name },
                            { "description", tool.Description },
                            { "input_schema", tool.InputSchema ?? new Dictionary<string, object>() }
                        });
                    }
                }

                if (isGemini)
                {
                    // Gemini wraps tools in a different structure
                    requestDict["tools"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "functionDeclarations", toolsArray }
                        }
                    };
                }
                else
                {
                    requestDict["tools"] = toolsArray;

                    // For Claude, encourage tool use
                    if (!isOllamaOrOpenAI)
                    {
                        requestDict["tool_choice"] = new Dictionary<string, object>
                        {
                            { "type", "auto" }
                        };
                    }
                }
            }

            var requestBody = JsonConvert.SerializeObject(requestDict);

            // Build URL (replace {model} placeholder for Gemini)
            string requestUrl = config.apiUrl;

            // Replace localhost with 127.0.0.1 for better Unity compatibility
            if (requestUrl.Contains("localhost"))
            {
                requestUrl = requestUrl.Replace("localhost", "127.0.0.1");
            }

            if (isGemini)
            {
                requestUrl = requestUrl.Replace("{model}", config.GetCurrentModel());
                // Add API key as URL parameter for Gemini
                requestUrl += $"?key={config.apiKey}";
            }

            // Log the request
            GameSmithLogger.LogRequest(config.activeProvider, requestUrl, requestBody);

            using (var request = new UnityWebRequest(requestUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");

                // Set headers based on provider
                if (config.activeProvider.Contains("Anthropic"))
                {
                    // Use version from config, fallback to 2023-06-01 if not specified
                    string apiVersion = !string.IsNullOrEmpty(config.apiVersion) ? config.apiVersion : "2023-06-01";
                    request.SetRequestHeader("anthropic-version", apiVersion);
                    request.SetRequestHeader("x-api-key", config.apiKey);
                }
                else if (config.activeProvider.Contains("OpenAI"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                }
                else if (config.activeProvider.Contains("Gemini"))
                {
                    // API key is in URL parameter
                }
                else if (config.activeProvider.Contains("Grok"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                }
                else if (config.activeProvider.Contains("Ollama"))
                {
                    // No authentication required
                }

                request.timeout = 120;

                // Add redirect handling
                request.redirectLimit = 5;

                // Send request
                var operation = request.SendWebRequest();

                // Wait for request to complete
                while (!request.isDone)
                {
                    yield return null;
                }

                // Request completed

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    GameSmithLogger.LogResponse(config.activeProvider, true, responseText);

                    AIResponse response = null;

                    // Parse based on provider type
                    if (isGemini)
                    {
                        response = ParseGeminiResponse(responseText);
                    }
                    else if (isOllamaOrOpenAI)
                    {
                        response = ParseOpenAIResponse(responseText);
                    }
                    else
                    {
                        response = ParseClaudeResponse(responseText);
                    }

                    if (response != null)
                    {
                        // Add assistant response to history
                        if (isGemini)
                        {
                            // For Gemini with tool use, store the function call
                            if (response.HasToolUse)
                            {
                                lastToolName = response.ToolName; // Track tool name for function response
                                var parts = new List<object>();
                                if (!string.IsNullOrEmpty(response.TextContent))
                                {
                                    parts.Add(new Dictionary<string, object> { { "text", response.TextContent } });
                                }
                                parts.Add(new Dictionary<string, object>
                                {
                                    { "functionCall", new Dictionary<string, object>
                                        {
                                            { "name", response.ToolName },
                                            { "args", response.ToolInput }
                                        }
                                    }
                                });
                                conversationHistory.Add(new Dictionary<string, object>
                                {
                                    { "role", "assistant" },
                                    { "content", parts }
                                });
                            }
                            else
                            {
                                conversationHistory.Add(new Dictionary<string, object>
                                {
                                    { "role", "assistant" },
                                    { "content", response.TextContent }
                                });
                            }
                        }
                        else if (isOllamaOrOpenAI)
                        {
                            // For OpenAI with tool calls, store the tool call in history
                            if (response.HasToolUse)
                            {
                                var toolCall = new Dictionary<string, object>
                                {
                                    { "id", response.ToolUseId },
                                    { "type", "function" },
                                    { "function", new Dictionary<string, object>
                                        {
                                            { "name", response.ToolName },
                                            { "arguments", MiniJSON.Json.Serialize(response.ToolInput) }
                                        }
                                    }
                                };

                                conversationHistory.Add(new Dictionary<string, object>
                                {
                                    { "role", "assistant" },
                                    { "content", null },
                                    { "tool_calls", new List<object> { toolCall } }
                                });
                            }
                            else
                            {
                                conversationHistory.Add(new Dictionary<string, object>
                                {
                                    { "role", "assistant" },
                                    { "content", response.TextContent }
                                });
                            }
                        }
                        else
                        {
                            conversationHistory.Add(new Dictionary<string, object>
                            {
                                { "role", "assistant" },
                                { "content", response.ContentBlocks }
                            });
                        }

                        onSuccess?.Invoke(response);
                    }
                    else
                    {
                        string providerName = config?.activeProvider ?? "Unknown";
                        GameSmithLogger.LogError($"Failed to parse response from {providerName}. Response was: {responseText}");
                        string parseError = $"Failed to parse AI response from {providerName}.";
                        onError?.Invoke(parseError);
                    }
                }
                else
                {
                    // Get detailed error info
                    string errorDetails = $"HTTP Status: {request.responseCode}\n";
                    errorDetails += $"Result: {request.result}\n";
                    errorDetails += $"Error: {request.error ?? "(empty)"}";

                    string responseBody = request.downloadHandler?.text;

                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        errorDetails += $"\nResponse: {responseBody}";
                    }

                    // Add more diagnostics for connection errors
                    if (request.result == UnityWebRequest.Result.ConnectionError)
                    {
                        errorDetails += "\nDiagnostic: Connection failed - server may not be running or reachable";

                        // For Ollama, add specific checks
                        if (config.activeProvider.Contains("Ollama"))
                        {
                            errorDetails += $"\nOllama URL: {config.apiUrl}";
                            errorDetails += "\nPlease verify:";
                            errorDetails += "\n  1. Ollama is installed";
                            errorDetails += "\n  2. Ollama service is running (ollama serve)";
                            errorDetails += "\n  3. Server is accessible at http://localhost:11434";
                        }
                    }

                    // Safely get provider name
                    string providerName = "Unknown";
                    try
                    {
                        if (config != null && !string.IsNullOrEmpty(config.activeProvider))
                        {
                            providerName = config.activeProvider;
                        }
                    }
                    catch
                    {
                        // Use default if config fails
                    }

                    GameSmithLogger.LogResponse(providerName, false, responseBody, errorDetails);

                    // Create user-friendly error message
                    string userError = BuildUserFriendlyError(providerName, request.responseCode, request.error, responseBody);
                    GameSmithLogger.LogError(userError);

                    onError?.Invoke(userError);
                }
            }
        }

        private AIResponse ParseGeminiResponse(string json)
        {
            try
            {
                var response = JObject.Parse(json);

                var result = new AIResponse
                {
                    ContentBlocks = new List<object>()
                };

                var candidates = response["candidates"];
                if (candidates != null && candidates.HasValues)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate != null)
                    {
                        var content = firstCandidate["content"];
                        if (content != null)
                        {
                            var parts = content["parts"];
                            if (parts != null && parts.HasValues)
                            {
                                var textBuilder = new StringBuilder();
                                foreach (var part in parts)
                                {
                                    // Check for text content
                                    var text = part["text"];
                                    if (text != null)
                                    {
                                        textBuilder.Append(text.ToString());
                                    }
                                    // Check for function call (Gemini tool use)
                                    else
                                    {
                                        var functionCall = part["functionCall"];
                                        if (functionCall != null)
                                        {
                                            result.HasToolUse = true;
                                            result.ToolName = functionCall["name"]?.ToString() ?? "";

                                            var args = functionCall["args"];
                                            result.ToolInput = args != null ? args.ToObject<Dictionary<string, object>>() : new Dictionary<string, object>();
                                            result.ToolUseId = Guid.NewGuid().ToString(); // Gemini doesn't provide ID, generate one
                                        }
                                    }
                                }
                                result.TextContent = textBuilder.ToString();
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Failed to parse Gemini response: {ex.Message}");
                return null;
            }
        }

        private AIResponse ParseOpenAIResponse(string json)
        {
            try
            {
                var response = JObject.Parse(json);

                var result = new AIResponse
                {
                    ContentBlocks = new List<object>()
                };

                var choices = response["choices"];
                if (choices == null || !choices.HasValues)
                {
                    return null;
                }

                var firstChoice = choices[0];
                if (firstChoice == null)
                {
                    return null;
                }

                var message = firstChoice["message"];
                if (message == null)
                {
                    return null;
                }

                // Get text content - handle both string and null content
                var content = message["content"];
                if (content != null && content.Type != JTokenType.Null)
                {
                    string fullContent = content.ToString();

                        // Extract thinking content from <think> tags
                        var thinkPattern = @"<think>(.*?)</think>";
                        var thinkMatch = System.Text.RegularExpressions.Regex.Match(fullContent, thinkPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

                        if (thinkMatch.Success)
                        {
                            var thinkingText = thinkMatch.Groups[1].Value.Trim();
                            // Only set thinking content if it's not empty
                            if (!string.IsNullOrWhiteSpace(thinkingText))
                            {
                                result.ThinkingContent = thinkingText;
                            }
                            // Remove thinking from main content
                            result.TextContent = System.Text.RegularExpressions.Regex.Replace(fullContent, thinkPattern, "").Trim();
                        }
                        else
                        {
                            result.TextContent = fullContent;
                        }

                        // Decode Unicode escapes (common in Ollama responses)
                        if (!string.IsNullOrEmpty(result.TextContent))
                        {
                            result.TextContent = DecodeUnicodeEscapes(result.TextContent);
                        }
                        if (!string.IsNullOrEmpty(result.ThinkingContent))
                        {
                            result.ThinkingContent = DecodeUnicodeEscapes(result.ThinkingContent);
                        }

                        // Check if Ollama returned a tool call as JSON string in content
                        if (!string.IsNullOrEmpty(result.TextContent) && result.TextContent.Trim().StartsWith("{"))
                        {
                            try
                            {
                                var toolCallJson = JObject.Parse(result.TextContent);
                                if (toolCallJson["name"] != null)
                                {
                                    // This is a tool call from Ollama
                                    result.HasToolUse = true;
                                    result.ToolName = toolCallJson["name"].ToString();
                                    result.ToolUseId = System.Guid.NewGuid().ToString();

                                    var args = toolCallJson["arguments"];
                                    if (args != null)
                                    {
                                        if (args.Type == JTokenType.Object)
                                        {
                                            result.ToolInput = args.ToObject<Dictionary<string, object>>();
                                        }
                                        else if (args.Type == JTokenType.String)
                                        {
                                            result.ToolInput = JObject.Parse(args.ToString()).ToObject<Dictionary<string, object>>();
                                        }
                                        else
                                        {
                                            result.ToolInput = new Dictionary<string, object>();
                                        }
                                    }
                                    else
                                    {
                                        result.ToolInput = new Dictionary<string, object>();
                                    }

                                    // Clear text content since this was a tool call
                                    result.TextContent = "";
                                }
                            }
                            catch
                            {
                                // Not a valid tool call JSON, keep as text
                            }
                        }
                    }
                    else
                    {
                        result.TextContent = "";
                    }

                // Check for tool calls (function calls in OpenAI format)
                var toolCalls = message["tool_calls"];
                if (toolCalls != null && toolCalls.HasValues)
                {
                    var firstToolCall = toolCalls[0];
                    if (firstToolCall != null)
                    {
                        var function = firstToolCall["function"];
                        if (function != null)
                        {
                            result.HasToolUse = true;
                            result.ToolUseId = firstToolCall["id"]?.ToString() ?? "";
                            result.ToolName = function["name"]?.ToString() ?? "";

                            var argsToken = function["arguments"];
                            if (argsToken != null)
                            {
                                var argsString = argsToken.ToString();
                                try
                                {
                                    // Arguments might be an empty string "{}" or a JSON string
                                    if (!string.IsNullOrEmpty(argsString))
                                    {
                                        result.ToolInput = JObject.Parse(argsString).ToObject<Dictionary<string, object>>();
                                    }
                                    else
                                    {
                                        result.ToolInput = new Dictionary<string, object>();
                                    }
                                }
                                catch
                                {
                                    result.ToolInput = new Dictionary<string, object>();
                                }
                            }
                        }
                    }
                }

                // Don't require text content if we have a tool call
                // If there's only thinking content and no actual response or tool use, add a placeholder
                if (string.IsNullOrWhiteSpace(result.TextContent) && !result.HasToolUse && !string.IsNullOrEmpty(result.ThinkingContent))
                {
                    result.TextContent = "[Continuing analysis...]";
                }

                return result;
            }
            catch (Exception ex)
            {
                GameSmithLogger.LogError($"Failed to parse response: {ex.Message}");
                return null;
            }
        }

        private AIResponse ParseClaudeResponse(string json)
        {
            try
            {
                var response = JObject.Parse(json);

                var result = new AIResponse
                {
                    ContentBlocks = new List<object>()
                };

                var content = response["content"];
                if (content != null && content.HasValues)
                {
                    foreach (var item in content)
                    {
                        result.ContentBlocks.Add(item.ToObject<object>());

                        var type = item["type"]?.ToString() ?? "";

                        if (type == "text")
                        {
                            var text = item["text"];
                            if (text != null)
                            {
                                result.TextContent += text.ToString();
                            }
                        }
                        else if (type == "thinking")
                        {
                            var thinking = item["thinking"];
                            if (thinking != null)
                            {
                                result.ThinkingContent += thinking.ToString();
                            }
                        }
                        else if (type == "tool_use")
                        {
                            result.HasToolUse = true;
                            result.ToolUseId = item["id"]?.ToString() ?? "";
                            result.ToolName = item["name"]?.ToString() ?? "";

                            var input = item["input"];
                            result.ToolInput = input != null ? input.ToObject<Dictionary<string, object>>() : new Dictionary<string, object>();
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

        private string BuildUserFriendlyError(string provider, long statusCode, string error, string responseBody)
        {
            // Try to extract server message from response body first
            if (!string.IsNullOrEmpty(responseBody))
            {
                try
                {
                    var errorJson = JObject.Parse(responseBody);
                    var errorObj = errorJson["error"];

                    if (errorObj != null)
                    {
                        // Handle error as object with message field
                        var message = errorObj["message"];
                        if (message != null)
                        {
                            return message.ToString();
                        }

                        // Handle error as direct string
                        if (errorObj.Type == JTokenType.String)
                        {
                            return errorObj.ToString();
                        }
                    }
                }
                catch
                {
                    // Failed to parse, continue to fallback
                }
            }

            // Fallback to simple error messages
            string errorSafe = error ?? "Unknown error";

            // Connection errors
            if (statusCode == 0 || errorSafe.Contains("Connection refused") || errorSafe.Contains("Failed to connect"))
            {
                return $"Cannot connect to {provider}. Make sure the service is running.";
            }

            // Authentication errors
            if (statusCode == 401 || statusCode == 403)
            {
                return "Invalid or missing API key.";
            }

            // Not found errors
            if (statusCode == 404)
            {
                return $"Model not found. Check your model selection in Settings.";
            }

            // Rate limiting
            if (statusCode == 429)
            {
                return "Rate limit exceeded. Please wait and try again.";
            }

            // Timeout
            if (errorSafe.Contains("timeout") || errorSafe.Contains("Timeout"))
            {
                return "Request timed out. Try again or use a faster model.";
            }

            // Network errors
            if (errorSafe.Contains("Couldn't resolve host") || errorSafe.Contains("Could not resolve host"))
            {
                return "Cannot reach server. Check your internet connection.";
            }

            // Generic error
            return errorSafe;
        }
    }

    /// <summary>
    /// Represents an AI response with potential tool uses and thinking
    /// </summary>
    public class AIResponse
    {
        public string TextContent = "";
        public string ThinkingContent = "";
        public List<object> ContentBlocks = new List<object>();
        public bool HasToolUse = false;
        public string ToolUseId = "";
        public string ToolName = "";
        public Dictionary<string, object> ToolInput = new Dictionary<string, object>();
    }
}
