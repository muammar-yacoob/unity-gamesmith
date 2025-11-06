using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Non-blocking MCP client that won't freeze Unity
    /// </summary>
    public class MCPClientAsync : IDisposable
    {
        private Process serverProcess;
        private StreamWriter stdinWriter;
        private StreamReader stdoutReader;
        private StreamReader stderrReader;
        private int nextRequestId = 1;
        private bool isInitializing = false;
        private bool isInitialized = false;
        private Queue<Action<bool>> initCallbacks = new Queue<Action<bool>>();

        public List<MCPTool> AvailableTools { get; private set; } = new List<MCPTool>();

        public bool IsConnected
        {
            get
            {
                if (serverProcess == null) return false;
                try
                {
                    return !serverProcess.HasExited && isInitialized;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Start MCP server process asynchronously
        /// </summary>
        public void StartServerAsync(string command, string[] args, Action<bool> onComplete = null)
        {
            if (onComplete != null)
                initCallbacks.Enqueue(onComplete);

            if (isInitializing)
            {
                UnityEngine.Debug.Log("[GameSmith MCP] Server initialization already in progress");
                return;
            }

            isInitializing = true;

            // Start the initialization coroutine
            EditorCoroutineRunner.StartCoroutine(StartServerCoroutine(command, args));
        }

        private IEnumerator StartServerCoroutine(string command, string[] args)
        {
            UnityEngine.Debug.Log("[GameSmith MCP] Starting server asynchronously...");

            bool hasError = false;
            string errorMessage = null;

            // Try block without yield
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                // Add arguments
                var argumentListProperty = startInfo.GetType().GetProperty("ArgumentList");
                if (argumentListProperty != null)
                {
                    var argumentList = argumentListProperty.GetValue(startInfo);
                    var addMethod = argumentList.GetType().GetMethod("Add");
                    foreach (var arg in args)
                    {
                        addMethod.Invoke(argumentList, new object[] { arg });
                    }
                }
                else
                {
                    var quotedArgs = new List<string>();
                    foreach (var arg in args)
                    {
                        if (arg.Contains(" ") && !arg.StartsWith("\""))
                        {
                            quotedArgs.Add($"\"{arg}\"");
                        }
                        else
                        {
                            quotedArgs.Add(arg);
                        }
                    }
                    startInfo.Arguments = string.Join(" ", quotedArgs);
                }

                serverProcess = new Process { StartInfo = startInfo };

                bool started = serverProcess.Start();
                if (!started)
                {
                    errorMessage = $"Failed to start process: {command}";
                    hasError = true;
                }
                else
                {
                    stdinWriter = serverProcess.StandardInput;
                    stdoutReader = serverProcess.StandardOutput;
                    stderrReader = serverProcess.StandardError;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to start server: {ex.Message}";
                hasError = true;
            }

            // Handle errors outside try block
            if (hasError)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] {errorMessage}");
                NotifyInitCallbacks(false);
                isInitializing = false;
                yield break;
            }

            // Wait a frame for process to start
            yield return null;

            // Check if process crashed immediately
            if (serverProcess.HasExited)
            {
                UnityEngine.Debug.LogError("[GameSmith MCP] Process exited immediately");
                NotifyInitCallbacks(false);
                isInitializing = false;
                yield break;
            }

            // Send initialize request asynchronously
            var initRequest = new
            {
                jsonrpc = "2.0",
                id = nextRequestId++,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { } },
                    clientInfo = new
                    {
                        name = "unity-gamesmith",
                        version = "1.4.1"
                    }
                }
            };

            try
            {
                var json = MiniJSON.Json.Serialize(initRequest);
                stdinWriter.WriteLine(json);
                stdinWriter.Flush();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to send init request: {ex.Message}");
                NotifyInitCallbacks(false);
                isInitializing = false;
                yield break;
            }

            // Wait for response asynchronously
            float timeoutTime = Time.realtimeSinceStartup + 5f; // 5 second timeout
            string response = null;

            while (Time.realtimeSinceStartup < timeoutTime)
            {
                try
                {
                    if (stdoutReader.Peek() >= 0)
                    {
                        response = stdoutReader.ReadLine();
                        if (!string.IsNullOrEmpty(response))
                            break;
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Error reading response: {ex.Message}");
                    break;
                }

                if (serverProcess.HasExited)
                {
                    UnityEngine.Debug.LogError("[GameSmith MCP] Server process exited during initialization");
                    NotifyInitCallbacks(false);
                    isInitializing = false;
                    yield break;
                }

                yield return null; // Wait one frame
            }

            if (string.IsNullOrEmpty(response))
            {
                UnityEngine.Debug.LogError("[GameSmith MCP] Initialization timeout");
                NotifyInitCallbacks(false);
                isInitializing = false;
                yield break;
            }

            try
            {
                var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                if (responseObj != null && responseObj.ContainsKey("result"))
                {
                    isInitialized = true;
                    UnityEngine.Debug.Log("[GameSmith MCP] Server initialized successfully");

                    // List tools asynchronously
                    EditorCoroutineRunner.StartCoroutine(ListToolsCoroutine());

                    NotifyInitCallbacks(true);
                }
                else
                {
                    UnityEngine.Debug.LogError("[GameSmith MCP] Invalid initialization response");
                    NotifyInitCallbacks(false);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to parse response: {ex.Message}");
                NotifyInitCallbacks(false);
            }

            isInitializing = false;
        }

        private IEnumerator ListToolsCoroutine()
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = nextRequestId++,
                method = "tools/list"
            };

            try
            {
                var json = MiniJSON.Json.Serialize(request);
                stdinWriter.WriteLine(json);
                stdinWriter.Flush();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to send tools/list request: {ex.Message}");
                yield break;
            }

            // Wait for response
            float timeoutTime = Time.realtimeSinceStartup + 3f;
            string response = null;

            while (Time.realtimeSinceStartup < timeoutTime)
            {
                try
                {
                    if (stdoutReader.Peek() >= 0)
                    {
                        response = stdoutReader.ReadLine();
                        if (!string.IsNullOrEmpty(response))
                            break;
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Error reading tools response: {ex.Message}");
                    break;
                }
                yield return null;
            }

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    if (responseObj != null && responseObj.ContainsKey("result"))
                    {
                        var result = responseObj["result"] as Dictionary<string, object>;
                        if (result != null && result.ContainsKey("tools"))
                        {
                            var toolsList = result["tools"] as List<object>;
                            if (toolsList != null)
                            {
                                AvailableTools.Clear();
                                foreach (var toolObj in toolsList)
                                {
                                    var tool = toolObj as Dictionary<string, object>;
                                    if (tool != null)
                                    {
                                        AvailableTools.Add(new MCPTool
                                        {
                                            Name = tool.ContainsKey("name") ? tool["name"].ToString() : "",
                                            Description = tool.ContainsKey("description") ? tool["description"].ToString() : "",
                                            InputSchema = tool.ContainsKey("inputSchema") ? tool["inputSchema"] : null
                                        });
                                    }
                                }
                                UnityEngine.Debug.Log($"[GameSmith MCP] Loaded {AvailableTools.Count} tools");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to parse tools: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Call an MCP tool asynchronously
        /// </summary>
        public void CallToolAsync(string toolName, Dictionary<string, object> arguments, Action<string> onResult)
        {
            if (!isInitialized || !IsConnected)
            {
                onResult?.Invoke("MCP server not connected");
                return;
            }

            EditorCoroutineRunner.StartCoroutine(CallToolCoroutine(toolName, arguments, onResult));
        }

        private IEnumerator CallToolCoroutine(string toolName, Dictionary<string, object> arguments, Action<string> onResult)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = nextRequestId++,
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = arguments
                }
            };

            var json = MiniJSON.Json.Serialize(request);
            stdinWriter.WriteLine(json);
            stdinWriter.Flush();

            // Wait for response
            float timeoutTime = Time.realtimeSinceStartup + 5f;
            string response = null;

            while (Time.realtimeSinceStartup < timeoutTime)
            {
                if (stdoutReader.Peek() >= 0)
                {
                    response = stdoutReader.ReadLine();
                    if (!string.IsNullOrEmpty(response))
                        break;
                }
                yield return null;
            }

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    if (responseObj != null && responseObj.ContainsKey("result"))
                    {
                        var result = responseObj["result"] as Dictionary<string, object>;
                        if (result != null && result.ContainsKey("content"))
                        {
                            var contentList = result["content"] as List<object>;
                            if (contentList != null && contentList.Count > 0)
                            {
                                var firstContent = contentList[0] as Dictionary<string, object>;
                                if (firstContent != null && firstContent.ContainsKey("text"))
                                {
                                    onResult?.Invoke(firstContent["text"].ToString());
                                    yield break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    onResult?.Invoke($"Error parsing response: {ex.Message}");
                }
            }
            else
            {
                onResult?.Invoke("Tool execution timeout");
            }
        }

        // Compatibility method for existing code
        public string CallTool(string toolName, Dictionary<string, object> arguments)
        {
            string result = null;
            bool completed = false;

            CallToolAsync(toolName, arguments, (r) =>
            {
                result = r;
                completed = true;
            });

            // Wait for completion (with timeout)
            float timeoutTime = Time.realtimeSinceStartup + 5f;
            while (!completed && Time.realtimeSinceStartup < timeoutTime)
            {
                // This is still blocking, but at least has a timeout
                System.Threading.Thread.Yield();
            }

            return result ?? "Tool execution failed or timed out";
        }

        private void NotifyInitCallbacks(bool success)
        {
            while (initCallbacks.Count > 0)
            {
                var callback = initCallbacks.Dequeue();
                callback?.Invoke(success);
            }
        }

        public void Dispose()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                try
                {
                    stdinWriter?.Close();
                    serverProcess.Kill();
                    serverProcess.Dispose();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Error disposing: {ex.Message}");
                }
            }
        }
    }
}