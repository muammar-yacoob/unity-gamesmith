using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
                // Special handling for unity-mcp - run with node directly
                bool isUnityMcp = false;
                if (command == "npx" && args.Length > 0 && (args[0] == "unity-mcp" || args[0] == "@spark-apps/unity-mcp"))
                {
                    isUnityMcp = true;
                }
                // Also handle direct node execution with path to unity-mcp
                else if ((command == "node" || command == "node.exe") && args.Length > 0 && args[0].Contains("unity-mcp"))
                {
                    isUnityMcp = true;
                }

                if (isUnityMcp)
                {
                    // Check if we already have a direct path to index.js
                    bool hasDirectPath = args.Length > 0 && args[0].EndsWith("index.js");

                    if (hasDirectPath)
                    {
                        // We already have the path, just fix the node command for Windows
                        if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                        {
                            command = @"C:\Program Files\nodejs\node.exe";
                            if (!System.IO.File.Exists(command))
                            {
                                command = "node.exe";
                            }
                        }
                        UnityEngine.Debug.Log($"[GameSmith MCP] Using provided path: {command} {args[0]}");
                    }
                    else
                    {
                        // Find global npm path
                        string globalNpmPath = null;
                        if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                        {
                            globalNpmPath = System.IO.Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "npm", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"
                            );

                            // Use full path to node.exe
                            command = @"C:\Program Files\nodejs\node.exe";
                            if (!System.IO.File.Exists(command))
                            {
                                // Fallback to just node.exe if not in standard location
                                command = "node.exe";
                            }
                        }
                        else
                        {
                            // Unix/Mac: try common global paths
                            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            globalNpmPath = System.IO.Path.Combine(
                                home, ".npm-global", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"
                            );
                            if (!System.IO.File.Exists(globalNpmPath))
                            {
                                globalNpmPath = System.IO.Path.Combine(
                                    "/usr", "lib", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"
                                );
                            }
                            command = "node";
                        }

                        if (globalNpmPath != null && System.IO.File.Exists(globalNpmPath))
                        {
                            args = new[] { globalNpmPath };
                            UnityEngine.Debug.Log($"[GameSmith MCP] Using direct node execution: {command} {globalNpmPath}");
                        }
                        else
                        {
                            // Fallback: use npx with full path
                            if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                            {
                                command = @"C:\Program Files\nodejs\npx.cmd";
                                if (!System.IO.File.Exists(command))
                                {
                                    // Fallback to just npx.cmd
                                    command = "npx.cmd";
                                }
                            }
                            else
                            {
                                command = "npx";
                            }
                            args = new[] { "@spark-apps/unity-mcp" };
                            UnityEngine.Debug.Log($"[GameSmith MCP] Using npx fallback: {command} {string.Join(" ", args)}");
                        }
                    }
                }
                // Fix command for Windows
                else if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                {
                    if (command == "npx")
                    {
                        command = @"C:\Program Files\nodejs\npx.cmd";
                        if (!System.IO.File.Exists(command))
                        {
                            command = "npx.cmd";
                        }
                    }
                    else if (command == "node" && !command.EndsWith(".exe"))
                    {
                        command = @"C:\Program Files\nodejs\node.exe";
                        if (!System.IO.File.Exists(command))
                        {
                            command = "node.exe";
                        }
                    }
                }

                // Validate command exists before attempting to start
                if (!System.IO.File.Exists(command))
                {
                    UnityEngine.Debug.LogWarning($"[GameSmith MCP] Command not found at: {command}, trying PATH resolution");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath) // Unity project root
                };

                // Add arguments
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }

                // Log the full command for debugging
                UnityEngine.Debug.Log($"[GameSmith MCP] Starting MCP server: {command} {string.Join(" ", args)}");

                serverProcess = new Process { StartInfo = startInfo };

                if (!serverProcess.Start())
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to start process: {command}");
                    return false;
                }

                stdinWriter = serverProcess.StandardInput;
                stdoutReader = serverProcess.StandardOutput;
                stderrReader = serverProcess.StandardError;

                // Start error reader task
                _ = ReadStderrAsync();

                // Wait a bit for process to initialize
                await UniTask.Delay(1000);

                bool hasExited = false;
                try
                {
                    hasExited = serverProcess.HasExited;
                }
                catch (InvalidOperationException)
                {
                    hasExited = true; // Process was disposed
                }

                if (hasExited)
                {
                    string stderr = "";
                    try
                    {
                        stderr = stderrReader.ReadToEnd();
                    }
                    catch { }

                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Process exited immediately. Exit code: {serverProcess.ExitCode}");
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        UnityEngine.Debug.LogError($"[GameSmith MCP] Stderr: {stderr}");
                    }
                    return false;
                }

                // Send initialize request - build proper JSON manually
                var json = string.Format(
                    "{{\"jsonrpc\":\"2.0\",\"id\":{0},\"method\":\"initialize\",\"params\":{{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{{\"tools\":{{}}}},\"clientInfo\":{{\"name\":\"unity-gamesmith\",\"version\":\"1.4.1\"}}}}}}",
                    nextRequestId++
                );

                // Debug log the request
                // Sending init request silently

                // Write on thread pool
                try
                {
                    stdinWriter.WriteLine(json);
                    stdinWriter.Flush();
                }
                catch (Exception writeEx)
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to write init request: {writeEx.Message}");
                    return false;
                }

                // Read response with timeout
                string response = null;
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        var readTask = ReadLineAsync(stdoutReader, cts.Token);
                        response = await readTask;
                    }
                }
                catch (Exception readEx)
                {
                    await UniTask.SwitchToMainThread();
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Read error: {readEx.Message}");
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

                UnityEngine.Debug.Log($"[GameSmith MCP] Init response: {response}");

                var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                if (responseObj != null && responseObj.ContainsKey("result"))
                {
                    UnityEngine.Debug.Log("[GameSmith MCP] Init successful, requesting tools list...");
                    await ListToolsAsync();
                    // Success logged by caller
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Invalid init response - responseObj is null or missing result. Response: {response}");
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

        /// <summary>
        /// Continuously reads stderr and logs any errors
        /// </summary>
        private async UniTaskVoid ReadStderrAsync()
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                while (serverProcess != null)
                {
                    try
                    {
                        if (serverProcess.HasExited)
                            break;
                    }
                    catch (InvalidOperationException)
                    {
                        // Process was disposed
                        break;
                    }

                    string line = await stderrReader.ReadLineAsync();
                    if (line == null)
                        break; // Stream closed

                    if (!string.IsNullOrEmpty(line))
                    {
                        await UniTask.SwitchToMainThread();
                        // Log all stderr output for debugging
                        if (line.Contains("Error") || line.Contains("error") || line.Contains("Exception"))
                        {
                            UnityEngine.Debug.LogError($"[MCP stderr] {line}");
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"[MCP stderr] {line}");
                        }
                        await UniTask.SwitchToThreadPool();
                    }
                }
            }
            catch (Exception ex)
            {
                // Only log if process is still valid
                try
                {
                    if (serverProcess != null && !serverProcess.HasExited)
                    {
                        await UniTask.SwitchToMainThread();
                        UnityEngine.Debug.LogError($"[MCP] stderr read error: {ex.Message}");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process was disposed, ignore
                }
            }
        }

        private async UniTask ListToolsAsync()
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                var json = string.Format(
                    @"{{""jsonrpc"":""2.0"",""id"":{0},""method"":""tools/list""}}",
                    nextRequestId++
                );
                stdinWriter.WriteLine(json);
                stdinWriter.Flush();

                // Read response with timeout
                string response = null;
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        response = await ReadLineAsync(stdoutReader, cts.Token);
                    }

                    if (string.IsNullOrEmpty(response))
                    {
                        await UniTask.SwitchToMainThread();
                        UnityEngine.Debug.LogError("[GameSmith MCP] ListTools timeout");
                        return;
                    }
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

                // Log response length for debugging
                UnityEngine.Debug.Log($"[MCP] tools/list response length: {response.Length} chars");

                // Save response to file for debugging
                try
                {
                    var debugPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "mcp_response.json");
                    System.IO.File.WriteAllText(debugPath, response);
                    UnityEngine.Debug.Log($"[MCP] Response saved to: {debugPath}");
                }
                catch { }

                // Try to deserialize the actual response
                object deserializedObj = null;
                try
                {
                    deserializedObj = MiniJSON.Json.Deserialize(response);
                    UnityEngine.Debug.Log($"[MCP] Deserialized type: {deserializedObj?.GetType()?.ToString() ?? "null"}");

                    if (deserializedObj == null)
                    {
                        // Try to find where parsing fails - test progressively smaller substrings
                        UnityEngine.Debug.LogError("[MCP] Parsing returned null. Testing substrings...");

                        // Try first 1000 chars
                        var test1000 = response.Substring(0, Math.Min(1000, response.Length));
                        var result1000 = MiniJSON.Json.Deserialize(test1000);
                        UnityEngine.Debug.Log($"[MCP] First 1000 chars parse: {result1000 != null}");

                        // Log first and last 200 chars
                        UnityEngine.Debug.LogError($"[MCP] First 200: {response.Substring(0, Math.Min(200, response.Length))}");
                        if (response.Length > 200)
                        {
                            UnityEngine.Debug.LogError($"[MCP] Last 200: {response.Substring(Math.Max(0, response.Length - 200))}");
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    UnityEngine.Debug.LogError($"[MCP] JSON parse exception: {parseEx.Message}\nStack: {parseEx.StackTrace}");
                    return;
                }

                var responseObj = deserializedObj as Dictionary<string, object>;
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
                            UnityEngine.Debug.Log($"[MCP] Parsed {AvailableTools.Count} tools from response");
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"[MCP] tools/list response: tools is not a list. Type: {result["tools"]?.GetType()}");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[MCP] tools/list response missing 'tools' key. Keys: {(result != null ? string.Join(", ", result.Keys) : "null")}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[MCP] tools/list invalid response. Keys: {(responseObj != null ? string.Join(", ", responseObj.Keys) : "null")}");
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
                // Build JSON manually for tool call
                var argsJson = MiniJSON.Json.Serialize(arguments ?? new Dictionary<string, object>());
                var json = string.Format(
                    @"{{""jsonrpc"":""2.0"",""id"":{0},""method"":""tools/call"",""params"":{{""name"":""{1}"",""arguments"":{2}}}}}",
                    nextRequestId++,
                    toolName,
                    argsJson
                );
                stdinWriter.WriteLine(json);
                stdinWriter.Flush();

                // Read response with timeout
                string response = null;
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        response = await ReadLineAsync(stdoutReader, cts.Token);
                    }

                    if (string.IsNullOrEmpty(response))
                    {
                        await UniTask.SwitchToMainThread();
                        UnityEngine.Debug.LogError("[GameSmith MCP] CallTool timeout");
                        return "Tool execution timeout";
                    }
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

        private async UniTask<string> ReadLineAsync(StreamReader reader, CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource<string>();

            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    string line = await reader.ReadLineAsync();
                    tcs.TrySetResult(line);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, configureAwait: false).Forget();

            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }

        public void Dispose()
        {
            bool shouldKill = false;
            try
            {
                shouldKill = serverProcess != null && !serverProcess.HasExited;
            }
            catch (InvalidOperationException)
            {
                // Process was disposed, don't kill
            }

            if (shouldKill)
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
