using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
                if (string.IsNullOrEmpty(command) || args == null)
                {
                    UnityEngine.Debug.LogError("[GameSmith MCP] Invalid command or arguments");
                    callback?.Invoke(false);
                    return;
                }

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
                        // Using provided path
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
                            // Using direct node execution
                        }
                        else
                        {
                            // Fallback: use npx with full path
                            if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                            {
                                command = System.IO.Path.Combine(
                                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
                                    "nodejs", "npx.cmd"
                                );
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
                            // Use the full global path with npx to avoid working directory issues
                            var globalUnityMcpPath = System.IO.Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                                "npm", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"
                            );
                            
                            if (System.IO.File.Exists(globalUnityMcpPath))
                            {
                                // Use node directly with the full path to avoid npx working directory issues
                                command = System.IO.Path.Combine(
                                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
                                    "nodejs", "node.exe"
                                );
                                args = new[] { globalUnityMcpPath };
                                    // Using direct node path
                            }
                            else
                            {
                                args = new[] { "@spark-apps/unity-mcp" };
                                // Using npx fallback
                            }
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
                    WorkingDirectory = Directory.GetCurrentDirectory() // Unity project directory
                };

                // Add arguments
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }

                // Starting MCP server

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

                var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                if (responseObj != null && responseObj.ContainsKey("result"))
                {
                    // Init successful, requesting tools list
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
                        // Only log actual errors, not informational messages
                        if (line.Contains("Error") || line.Contains("error") || line.Contains("Exception"))
                        {
                            await UniTask.SwitchToMainThread();
                            UnityEngine.Debug.LogError($"[MCP] {line}");
                            await UniTask.SwitchToThreadPool();
                        }
                        // Ignore informational stderr messages like "Unity MCP Server initialized"
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

                // Try to deserialize the actual response
                object deserializedObj = null;
                try
                {
                    deserializedObj = MiniJSON.Json.Deserialize(response);

                    if (deserializedObj == null)
                    {
                        // MiniJSON failed, try manual extraction
                        
                        try
                        {
                            // Enhanced regex to extract tool names and descriptions
                            var toolPattern = @"""name""\s*:\s*""([^""]+)""[^}]*?""description""\s*:\s*""([^""]*?)""";
                            var toolMatches = Regex.Matches(response, toolPattern, RegexOptions.Singleline);
                            AvailableTools.Clear();
                            
                            foreach (Match match in toolMatches)
                            {
                                if (match.Groups.Count > 2)
                                {
                                    var toolName = match.Groups[1].Value;
                                    var toolDescription = match.Groups[2].Value;
                                    
                                    AvailableTools.Add(new MCPTool
                                    {
                                        Name = toolName,
                                        Description = !string.IsNullOrEmpty(toolDescription) ? toolDescription : $"Unity MCP tool: {toolName}",
                                        InputSchema = new Dictionary<string, object>
                                        {
                                            { "type", "object" },
                                            { "properties", new Dictionary<string, object>() }
                                        }
                                    });
                                }
                            }
                            
                            // Fallback: if no matches with description, try name-only
                            if (AvailableTools.Count == 0)
                            {
                                var nameMatches = Regex.Matches(response, @"""name""\s*:\s*""([^""]+)""");
                                foreach (Match match in nameMatches)
                                {
                                    if (match.Groups.Count > 1)
                                    {
                                        var toolName = match.Groups[1].Value;
                                        AvailableTools.Add(new MCPTool
                                        {
                                            Name = toolName,
                                            Description = $"Unity MCP tool: {toolName}",
                                            InputSchema = new Dictionary<string, object>
                                            {
                                                { "type", "object" },
                                                { "properties", new Dictionary<string, object>() }
                                            }
                                        });
                                    }
                                }
                            }
                            
                            return; // Exit early since we got the tools
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"[MCP] Manual extraction failed: {ex.Message}");
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
                                        InputSchema = tool.ContainsKey("inputSchema") ? tool["inputSchema"] : new Dictionary<string, object>
                                        {
                                            { "type", "object" },
                                            { "properties", new Dictionary<string, object>() }
                                        }
                                    });
                                }
                            }
                            UnityEngine.Debug.Log($"[MCP] Connected with {AvailableTools.Count} tools");
                            // Log tool names for debugging
                            if (AvailableTools.Count > 0)
                            {
                                var toolNames = string.Join(", ", AvailableTools.Select(t => t.Name).Take(10));
                                UnityEngine.Debug.Log($"[MCP] Available tools (first 10): {toolNames}");
                            }
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
            // Check if server is still running
            if (!IsConnected)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Server not connected when trying to call tool: {toolName}");
                return "Error: MCP server is not connected";
            }
            
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
                UnityEngine.Debug.Log($"[GameSmith MCP] Sending: {json}");
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

                UnityEngine.Debug.Log($"[GameSmith MCP] Received: {response}");
                await UniTask.SwitchToMainThread();

                var responseObj = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                
                // Check for MCP-level errors first
                if (responseObj != null && responseObj.ContainsKey("error"))
                {
                    var error = responseObj["error"] as Dictionary<string, object>;
                    if (error != null && error.ContainsKey("message"))
                    {
                        return $"MCP Error: {error["message"]}";
                    }
                    return "MCP Error: Unknown error";
                }
                
                // Parse successful result
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
                                string textContent = firstContent["text"].ToString();
                                
                                // Check if the text content is JSON with an error
                                try
                                {
                                    var toolResult = MiniJSON.Json.Deserialize(textContent) as Dictionary<string, object>;
                                    if (toolResult != null && toolResult.ContainsKey("error"))
                                    {
                                        return $"Tool Error: {toolResult["error"]}";
                                    }
                                    if (toolResult != null && toolResult.ContainsKey("success") && 
                                        toolResult["success"].ToString().ToLower() == "false" && 
                                        toolResult.ContainsKey("error"))
                                    {
                                        return $"Tool Error: {toolResult["error"]}";
                                    }
                                }
                                catch
                                {
                                    // Not JSON, return as-is
                                }
                                
                                return textContent;
                            }
                        }
                    }
                }

                return "Tool execution failed - invalid response format";
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
            catch (ObjectDisposedException)
            {
                // Process was disposed, don't kill
                shouldKill = false;
            }
            catch (InvalidOperationException)
            {
                // Process was disposed, don't kill
                shouldKill = false;
            }

            if (shouldKill)
            {
                try
                {
                    stdinWriter?.Close();
                    serverProcess?.Kill();
                }
                catch (ObjectDisposedException)
                {
                    // Process already disposed
                }
                catch (InvalidOperationException)
                {
                    // Process already exited or disposed
                }
                catch { }
            }

            // Clean up resources
            try
            {
                stdinWriter?.Dispose();
                stdoutReader?.Dispose();
                stderrReader?.Dispose();
                serverProcess?.Dispose();
            }
            catch { }

            // Clear references
            stdinWriter = null;
            stdoutReader = null;
            stderrReader = null;
            serverProcess = null;
        }
    }
}
