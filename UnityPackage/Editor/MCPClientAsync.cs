using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// MCP (Model Context Protocol) client using UniTask for non-blocking async operations
    /// </summary>
    public class MCPClientAsync : IDisposable
    {
        private Process serverProcess;
        private StreamWriter stdinWriter;
        private StreamReader stdoutReader;
        private StreamReader stderrReader;
        private int nextRequestId = 1;
        private bool isInitialized = false;

        public List<MCPTool> AvailableTools { get; private set; } = new List<MCPTool>();

        public bool IsConnected
        {
            get
            {
                if (serverProcess == null) return false;
                try { return !serverProcess.HasExited; }
                catch { return false; }
            }
        }

        public async void StartServerAsync(string command, string[] args, Action<bool> callback)
        {
            try
            {
                bool success = await StartServerInternalAsync(command, args);
                callback?.Invoke(success);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Error: {ex.Message}");
                callback?.Invoke(false);
            }
        }

        private async UniTask<bool> StartServerInternalAsync(string command, string[] args)
        {
            // Switch to thread pool for process startup
            await UniTask.SwitchToThreadPool();

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Add arguments
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }

                serverProcess = new Process { StartInfo = startInfo };
                if (!serverProcess.Start())
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError("[GameSmith MCP] Failed to start process");
                    return false;
                }

                stdinWriter = serverProcess.StandardInput;
                stdoutReader = serverProcess.StandardOutput;
                stderrReader = serverProcess.StandardError;

                // Wait a bit for process to initialize
                await UniTask.Delay(500);

                if (serverProcess.HasExited)
                {
                    string stderr = await stderrReader.ReadToEndAsync();
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Process exited. Exit code: {serverProcess.ExitCode}. Stderr: {stderr}");
                    return false;
                }

                // Send initialize request
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

                var json = MiniJSON.Json.Serialize(initRequest);
                await stdinWriter.WriteLineAsync(json);
                await stdinWriter.FlushAsync();

                // Read response with timeout
                string response;
                try
                {
                    response = await stdoutReader.ReadLineAsync()
                        .AsUniTask()
                        .Timeout(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError("[GameSmith MCP] Initialization timeout");
                    return false;
                }

                if (string.IsNullOrEmpty(response))
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError("[GameSmith MCP] No response received");
                    return false;
                }

                // Switch to main thread for JSON parsing (Unity's JsonUtility)
                await UniTask.SwitchToMainThread();

                var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                if (responseObj != null && responseObj.ContainsKey("result"))
                {
                    isInitialized = true;
                    await ListToolsAsync();
                    UnityEngine.Debug.Log($"[GameSmith MCP] Server initialized with {AvailableTools.Count} tools");
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Invalid init response: {response}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                UnityEngine.Debug.LogError($"[GameSmith MCP] Exception: {ex.Message}");
                return false;
            }
        }

        private async UniTask ListToolsAsync()
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                var request = new
                {
                    jsonrpc = "2.0",
                    id = nextRequestId++,
                    method = "tools/list"
                };

                var json = MiniJSON.Json.Serialize(request);
                await stdinWriter.WriteLineAsync(json);
                await stdinWriter.FlushAsync();

                // Read response with timeout
                string response;
                try
                {
                    response = await stdoutReader.ReadLineAsync()
                        .AsUniTask()
                        .Timeout(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError("[GameSmith MCP] ListTools timeout");
                    return;
                }

                if (string.IsNullOrEmpty(response))
                {
                    await UniTask.SwitchToMainThread();
                    return;
                }

                await UniTask.SwitchToMainThread();

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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                UnityEngine.Debug.LogError($"[GameSmith MCP] ListTools error: {ex.Message}");
            }
        }

        public async void CallToolAsync(string toolName, Dictionary<string, object> arguments, Action<string> callback)
        {
            try
            {
                string result = await CallToolInternalAsync(toolName, arguments);
                callback?.Invoke(result);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] CallTool error: {ex.Message}");
                callback?.Invoke($"Error: {ex.Message}");
            }
        }

        private async UniTask<string> CallToolInternalAsync(string toolName, Dictionary<string, object> arguments)
        {
            await UniTask.SwitchToThreadPool();

            try
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
                await stdinWriter.WriteLineAsync(json);
                await stdinWriter.FlushAsync();

                // Read response with timeout
                string response;
                try
                {
                    response = await stdoutReader.ReadLineAsync()
                        .AsUniTask()
                        .Timeout(TimeSpan.FromSeconds(30));
                }
                catch (TimeoutException)
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError("[GameSmith MCP] CallTool timeout");
                    return "Tool execution timeout";
                }

                if (string.IsNullOrEmpty(response))
                {
                    await UniTask.SwitchToMainThread();
                    return "No response";
                }

                await UniTask.SwitchToMainThread();

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
                                return firstContent["text"].ToString();
                            }
                        }
                    }
                }

                return "Tool execution failed";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
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
                catch { }
            }
        }
    }
}
