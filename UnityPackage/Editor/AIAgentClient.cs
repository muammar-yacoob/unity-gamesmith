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
            GameSmithLogger.Log($"SendMessage called for provider: {config.activeProvider}");

            if (!config.IsValid())
            {
                string validationError = config.GetValidationMessage();
                GameSmithLogger.LogWarning($"Config validation failed: {validationError}");
                onError?.Invoke(validationError);
                return;
            }

            GameSmithLogger.Log($"Using model: {config.GetCurrentModel()} at {config.apiUrl}");

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
            bool isOllamaOrOpenAI = config.activeProvider == "Ollama" || config.activeProvider == "OpenAI";
            bool isGemini = config.activeProvider == "Gemini";

            // For Ollama on localhost, normalize the URL
            if (config.activeProvider == "Ollama" && config.apiUrl.Contains("localhost"))
            {
                GameSmithLogger.Log("Detected localhost URL for Ollama, will use 127.0.0.1 for better Unity compatibility");
            }

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
                        string content = msgDict["content"].ToString();

                        contents.Add(new Dictionary<string, object>
                        {
                            { "role", role == "assistant" ? "model" : "user" },
                            { "parts", new List<object>
                                {
                                    new Dictionary<string, object> { { "text", content } }
                                }
                            }
                        });
                    }
                }

                requestDict["contents"] = contents;
                requestDict["generationConfig"] = new Dictionary<string, object>
                {
                    { "temperature", config.temperature },
                    { "maxOutputTokens", config.maxTokens }
                };
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

            // Add tools if available (both formats support this)
            if (tools != null && tools.Count > 0)
            {
                var toolsArray = new List<object>();
                foreach (var tool in tools)
                {
                    if (isOllamaOrOpenAI)
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
                requestDict["tools"] = toolsArray;
            }

            var requestBody = MiniJSON.Json.Serialize(requestDict);

            // Build URL (replace {model} placeholder for Gemini)
            string requestUrl = config.apiUrl;

            // Replace localhost with 127.0.0.1 for better Unity compatibility
            if (requestUrl.Contains("localhost"))
            {
                requestUrl = requestUrl.Replace("localhost", "127.0.0.1");
                GameSmithLogger.Log($"Normalized URL from localhost to 127.0.0.1: {requestUrl}");
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
                if (config.activeProvider == "Claude")
                {
                    request.SetRequestHeader("anthropic-version", "2023-06-01");
                    request.SetRequestHeader("x-api-key", config.apiKey);
                    GameSmithLogger.Log("Using Claude API with anthropic-version header");
                }
                else if (config.activeProvider == "OpenAI")
                {
                    request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                    GameSmithLogger.Log("Using OpenAI API with Bearer token");
                }
                else if (config.activeProvider == "Gemini")
                {
                    GameSmithLogger.Log("Using Gemini API (API key in URL parameter)");
                }
                else if (config.activeProvider == "Ollama")
                {
                    GameSmithLogger.Log("Using Ollama API (no authentication required)");
                }

                request.timeout = 120;

                // Add redirect handling
                request.redirectLimit = 5;

                GameSmithLogger.Log($"Sending web request to: {requestUrl}");
                GameSmithLogger.Log($"Request method: POST");
                GameSmithLogger.Log($"Content-Type: application/json");
                GameSmithLogger.Log($"Timeout: {request.timeout}s");

                // Send request
                var operation = request.SendWebRequest();

                // Wait for request to complete
                while (!request.isDone)
                {
                    yield return null;
                }

                GameSmithLogger.Log($"Request completed. Result: {request.result}, ResponseCode: {request.responseCode}, IsDone: {request.isDone}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    GameSmithLogger.LogResponse(config.activeProvider, true, responseText);
                    GameSmithLogger.Log("Request successful, parsing response...");

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
                        GameSmithLogger.Log($"Response parsed successfully. Text length: {response.TextContent?.Length ?? 0}");

                        // Add assistant response to history
                        if (isGemini || isOllamaOrOpenAI)
                        {
                            conversationHistory.Add(new Dictionary<string, object>
                            {
                                { "role", "assistant" },
                                { "content", response.TextContent }
                            });
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
                        string parseError = "Failed to parse AI response. Check log file for details.";
                        string providerName = config?.activeProvider ?? "Unknown";
                        GameSmithLogger.LogError($"Failed to parse response from {providerName}. Response was: {responseText}");
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
                        if (config.activeProvider == "Ollama")
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
                    GameSmithLogger.LogError($"Request failed: {userError}");

                    // Also log to Unity console for immediate visibility
                    UnityEngine.Debug.LogError($"[GameSmith] {providerName} request failed: {userError}");

                    onError?.Invoke(userError);
                }
            }
        }

        private AIResponse ParseGeminiResponse(string json)
        {
            try
            {
                var response = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                if (response == null) return null;

                var result = new AIResponse
                {
                    ContentBlocks = new List<object>()
                };

                if (response.ContainsKey("candidates"))
                {
                    var candidates = response["candidates"] as List<object>;
                    if (candidates != null && candidates.Count > 0)
                    {
                        var firstCandidate = candidates[0] as Dictionary<string, object>;
                        if (firstCandidate != null && firstCandidate.ContainsKey("content"))
                        {
                            var content = firstCandidate["content"] as Dictionary<string, object>;
                            if (content != null && content.ContainsKey("parts"))
                            {
                                var parts = content["parts"] as List<object>;
                                if (parts != null && parts.Count > 0)
                                {
                                    var textBuilder = new StringBuilder();
                                    foreach (var part in parts)
                                    {
                                        var partDict = part as Dictionary<string, object>;
                                        if (partDict != null && partDict.ContainsKey("text"))
                                        {
                                            textBuilder.Append(partDict["text"].ToString());
                                        }
                                    }
                                    result.TextContent = textBuilder.ToString();
                                }
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
                var response = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                if (response == null) return null;

                var result = new AIResponse
                {
                    ContentBlocks = new List<object>()
                };

                if (response.ContainsKey("choices"))
                {
                    var choices = response["choices"] as List<object>;
                    if (choices != null && choices.Count > 0)
                    {
                        var firstChoice = choices[0] as Dictionary<string, object>;
                        if (firstChoice != null && firstChoice.ContainsKey("message"))
                        {
                            var message = firstChoice["message"] as Dictionary<string, object>;
                            if (message != null)
                            {
                                // Get text content
                                if (message.ContainsKey("content"))
                                {
                                    result.TextContent = message["content"]?.ToString() ?? "";
                                }

                                // Check for tool calls (function calls in OpenAI format)
                                if (message.ContainsKey("tool_calls"))
                                {
                                    var toolCalls = message["tool_calls"] as List<object>;
                                    if (toolCalls != null && toolCalls.Count > 0)
                                    {
                                        var firstToolCall = toolCalls[0] as Dictionary<string, object>;
                                        if (firstToolCall != null && firstToolCall.ContainsKey("function"))
                                        {
                                            var function = firstToolCall["function"] as Dictionary<string, object>;
                                            if (function != null)
                                            {
                                                result.HasToolUse = true;
                                                result.ToolUseId = firstToolCall.ContainsKey("id") ? firstToolCall["id"].ToString() : "";
                                                result.ToolName = function.ContainsKey("name") ? function["name"].ToString() : "";

                                                if (function.ContainsKey("arguments"))
                                                {
                                                    var argsString = function["arguments"].ToString();
                                                    result.ToolInput = MiniJSON.Json.Deserialize(argsString) as Dictionary<string, object> ?? new Dictionary<string, object>();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Failed to parse OpenAI response: {ex.Message}");
                return null;
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

        private string BuildUserFriendlyError(string provider, long statusCode, string error, string responseBody)
        {
            var errorMsg = new System.Text.StringBuilder();

            // Get log file path safely
            string logFilePath = "Logs/GameSmith/";
            try
            {
                logFilePath = GameSmithLogger.GetLogFilePath() ?? "Logs/GameSmith/";
            }
            catch
            {
                // If logger fails, use default path
            }

            // Null-safe error checking
            string errorSafe = error ?? "";

            // Check for common Ollama issues first
            if (provider == "Ollama")
            {
                // HTTP Status 0 typically means connection error
                if (statusCode == 0 || errorSafe.Contains("Connection refused") || errorSafe.Contains("Failed to connect"))
                {
                    errorMsg.AppendLine("❌ Cannot connect to Ollama");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Ollama is not running or not accessible.");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Please ensure:");
                    errorMsg.AppendLine("1. Ollama is installed and running");
                    errorMsg.AppendLine("2. Ollama is listening on http://localhost:11434");
                    errorMsg.AppendLine("3. Try running: curl http://localhost:11434/api/tags");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                    return errorMsg.ToString();
                }

                if (statusCode == 404)
                {
                    string modelName = "the selected model";
                    try
                    {
                        if (config != null)
                        {
                            modelName = config.GetCurrentModel();
                        }
                    }
                    catch
                    {
                        // Use default if config fails
                    }

                    errorMsg.AppendLine("❌ Model not found in Ollama");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine($"The model '{modelName}' is not available.");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Please:");
                    errorMsg.AppendLine("1. Check available models: ollama list");
                    errorMsg.AppendLine($"2. Pull the model: ollama pull {modelName}");
                    errorMsg.AppendLine("3. Or select a different model in Settings");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                    return errorMsg.ToString();
                }
            }

            // Check for Gemini-specific issues
            if (provider == "Gemini")
            {
                if (statusCode == 400)
                {
                    if (errorSafe.Contains("API_KEY") || responseBody.Contains("API_KEY"))
                    {
                        errorMsg.AppendLine("❌ Invalid Gemini API Key");
                        errorMsg.AppendLine();
                        errorMsg.AppendLine("The API key format is incorrect or missing.");
                        errorMsg.AppendLine();
                        errorMsg.AppendLine("Please:");
                        errorMsg.AppendLine("1. Click Settings button");
                        errorMsg.AppendLine("2. Get a new API key from: https://aistudio.google.com/app/apikey");
                        errorMsg.AppendLine("3. Enter the key in the Gemini API Key field");
                        errorMsg.AppendLine();
                        errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                        return errorMsg.ToString();
                    }
                }

                if (statusCode == 404)
                {
                    string modelName = "the selected model";
                    try
                    {
                        if (config != null)
                        {
                            modelName = config.GetCurrentModel();
                        }
                    }
                    catch { }

                    errorMsg.AppendLine("❌ Gemini Model Not Found");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine($"The model '{modelName}' is not available or doesn't exist.");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Please:");
                    errorMsg.AppendLine("1. Check the model name is correct");
                    errorMsg.AppendLine("2. Try using 'gemini-1.5-pro-latest' or 'gemini-1.5-flash-latest'");
                    errorMsg.AppendLine("3. Select a different model in Settings");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                    return errorMsg.ToString();
                }

                if (statusCode == 429)
                {
                    errorMsg.AppendLine("❌ Gemini API Quota Exceeded");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("You've reached your API quota or rate limit.");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Please:");
                    errorMsg.AppendLine("1. Wait a moment before trying again");
                    errorMsg.AppendLine("2. Check your quota at: https://aistudio.google.com/app/apikey");
                    errorMsg.AppendLine("3. Consider upgrading your API plan if needed");
                    errorMsg.AppendLine();
                    errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                    return errorMsg.ToString();
                }
            }

            // Handle authentication errors
            if (statusCode == 401 || statusCode == 403)
            {
                errorMsg.AppendLine($"❌ Authentication Failed ({provider})");
                errorMsg.AppendLine();
                errorMsg.AppendLine("Your API key appears to be invalid or missing.");
                errorMsg.AppendLine();
                errorMsg.AppendLine("Please:");
                errorMsg.AppendLine("1. Click Settings button");
                errorMsg.AppendLine($"2. Verify your {provider} API key");
                errorMsg.AppendLine("3. Get a new key if needed");
                errorMsg.AppendLine();
                errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                return errorMsg.ToString();
            }

            // Handle network errors
            if (errorSafe.Contains("Couldn't resolve host") || errorSafe.Contains("Could not resolve host"))
            {
                string apiUrl = "the configured API URL";
                try
                {
                    if (config != null && !string.IsNullOrEmpty(config.apiUrl))
                    {
                        apiUrl = config.apiUrl;
                    }
                }
                catch
                {
                    // Use default if config fails
                }

                errorMsg.AppendLine("❌ Network Error - Cannot reach server");
                errorMsg.AppendLine();
                errorMsg.AppendLine("Cannot connect to the API endpoint.");
                errorMsg.AppendLine();
                errorMsg.AppendLine("Please check:");
                errorMsg.AppendLine("1. Your internet connection");
                errorMsg.AppendLine($"2. API URL in settings: {apiUrl}");
                errorMsg.AppendLine("3. Firewall/proxy settings");
                errorMsg.AppendLine();
                errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                return errorMsg.ToString();
            }

            // Handle timeout errors
            if (errorSafe.Contains("timeout") || errorSafe.Contains("Timeout"))
            {
                errorMsg.AppendLine("❌ Request Timeout");
                errorMsg.AppendLine();
                errorMsg.AppendLine("The request took too long to complete.");
                errorMsg.AppendLine();
                errorMsg.AppendLine("This could mean:");
                errorMsg.AppendLine("1. The server is slow or overloaded");
                errorMsg.AppendLine("2. Your internet connection is slow");
                errorMsg.AppendLine("3. The model is taking too long to respond");
                errorMsg.AppendLine();
                errorMsg.AppendLine("Try again or use a faster model.");
                errorMsg.AppendLine();
                errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                return errorMsg.ToString();
            }

            // Handle rate limiting
            if (statusCode == 429)
            {
                errorMsg.AppendLine($"❌ Rate Limit Exceeded ({provider})");
                errorMsg.AppendLine();
                errorMsg.AppendLine("You've made too many requests.");
                errorMsg.AppendLine();
                errorMsg.AppendLine("Please wait a moment and try again.");
                errorMsg.AppendLine();
                errorMsg.AppendLine($"📝 Check log file for details: {logFilePath}");
                return errorMsg.ToString();
            }

            // Generic error with details
            errorMsg.AppendLine($"❌ Request Failed ({provider ?? "Unknown"})");
            errorMsg.AppendLine();
            errorMsg.AppendLine($"HTTP Status: {statusCode}");
            errorMsg.AppendLine($"Error: {errorSafe}");

            // Try to parse error from response body
            if (!string.IsNullOrEmpty(responseBody))
            {
                try
                {
                    var errorJson = MiniJSON.Json.Deserialize(responseBody) as Dictionary<string, object>;
                    if (errorJson != null)
                    {
                        if (errorJson.ContainsKey("error"))
                        {
                            var errorObj = errorJson["error"];
                            if (errorObj is Dictionary<string, object> errDict)
                            {
                                if (errDict.ContainsKey("message"))
                                {
                                    errorMsg.AppendLine();
                                    errorMsg.AppendLine($"Server message: {errDict["message"]}");
                                }
                            }
                            else if (errorObj is string errStr)
                            {
                                errorMsg.AppendLine();
                                errorMsg.AppendLine($"Server message: {errStr}");
                            }
                        }
                    }
                }
                catch
                {
                    // Couldn't parse error, that's okay
                }
            }

            errorMsg.AppendLine();
            errorMsg.AppendLine($"📝 Full details in log file: {logFilePath}");

            return errorMsg.ToString();
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
