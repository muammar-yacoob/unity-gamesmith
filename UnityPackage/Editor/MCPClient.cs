using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// MCP (Model Context Protocol) client for communicating with MCP servers via stdio
    /// </summary>
    public class MCPClient : IDisposable
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
                try
                {
                    return !serverProcess.HasExited;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Start MCP server process and initialize connection
        /// </summary>
        public bool StartServer(string command, string[] args)
        {
            try
            {
                // Use the command directly (npx.cmd on Windows)
                var fileName = command;
                var arguments = string.Join(" ", args);

                serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Directory.GetCurrentDirectory() // Explicitly set working directory
                    }
                };

                bool started = serverProcess.Start();
                if (!started)
                {
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to start process: {command}");
                    serverProcess = null;
                    return false;
                }

                stdinWriter = serverProcess.StandardInput;
                stdoutReader = serverProcess.StandardOutput;
                stderrReader = serverProcess.StandardError;

                // Give the process a moment to start
                System.Threading.Thread.Sleep(500);

                // Check if process crashed immediately and log stderr
                if (serverProcess.HasExited)
                {
                    var stderr = stderrReader.ReadToEnd();
                    UnityEngine.Debug.LogError($"[GameSmith MCP] Process exited immediately. Error: {stderr}");
                    serverProcess = null;
                    return false;
                }

                // Check for early stderr messages
                if (stderrReader.Peek() > 0)
                {
                    var stderr = stderrReader.ReadToEnd();
                    UnityEngine.Debug.LogWarning($"[GameSmith MCP] Server stderr (startup): {stderr}");
                }

                // Initialize MCP protocol
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

                UnityEngine.Debug.Log("[GameSmith MCP] Sending initialize request...");
                var response = SendRequest(initRequest);

                if (response != null)
                {
                    isInitialized = true;

                    // List available tools
                    ListTools();

                    UnityEngine.Debug.Log($"[GameSmith MCP] Connected to server with {AvailableTools.Count} tools");
                    return true;
                }

                // Log stderr to see what went wrong
                if (!serverProcess.HasExited && stderrReader.Peek() > 0)
                {
                    var stderr = stderrReader.ReadLine();
                    UnityEngine.Debug.LogWarning($"[GameSmith MCP] Server stderr: {stderr}");
                }

                UnityEngine.Debug.LogWarning("[GameSmith MCP] Failed to initialize MCP protocol - no response received");
                Dispose();
                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to start server: {ex.Message}\nMake sure Node.js and npx are installed and in PATH.");
                if (serverProcess != null)
                {
                    try { serverProcess.Kill(); } catch { }
                    serverProcess = null;
                }
                return false;
            }
        }

        /// <summary>
        /// List available tools from MCP server
        /// </summary>
        private void ListTools()
        {
            try
            {
                var request = new
                {
                    jsonrpc = "2.0",
                    id = nextRequestId++,
                    method = "tools/list"
                };

                var response = SendRequest(request);
                if (response != null && response.ContainsKey("result"))
                {
                    var result = response["result"] as Dictionary<string, object>;
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
                UnityEngine.Debug.LogError($"[GameSmith MCP] Failed to list tools: {ex.Message}");
            }
        }

        /// <summary>
        /// Call an MCP tool
        /// </summary>
        public string CallTool(string toolName, Dictionary<string, object> arguments)
        {
            if (!isInitialized || !IsConnected)
            {
                return "MCP server not connected";
            }

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

                var response = SendRequest(request);
                if (response != null && response.ContainsKey("result"))
                {
                    var result = response["result"] as Dictionary<string, object>;
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
                return $"Error calling tool: {ex.Message}";
            }
        }

        /// <summary>
        /// Send JSON-RPC request and receive response
        /// </summary>
        private Dictionary<string, object> SendRequest(object request)
        {
            try
            {
                var json = MiniJSON.Json.Serialize(request);
                UnityEngine.Debug.Log($"[GameSmith MCP] Sending JSON: {json}");
                UnityEngine.Debug.Log($"[GameSmith MCP] JSON length: {json.Length} bytes");

                stdinWriter.WriteLine(json);
                stdinWriter.Flush();

                // Read response with timeout
                var startTime = System.DateTime.Now;
                int checkCount = 0;
                while ((System.DateTime.Now - startTime).TotalSeconds < 10)
                {
                    checkCount++;

                    // Check stderr for errors
                    if (stderrReader.Peek() >= 0)
                    {
                        var stderrLine = stderrReader.ReadLine();
                        UnityEngine.Debug.LogWarning($"[GameSmith MCP] Server stderr: {stderrLine}");
                    }

                    // Check stdout for response
                    if (stdoutReader.Peek() >= 0)
                    {
                        var responseLine = stdoutReader.ReadLine();
                        if (!string.IsNullOrEmpty(responseLine))
                        {
                            UnityEngine.Debug.Log($"[GameSmith MCP] Received: {responseLine}");
                            var response = MiniJSON.Json.Deserialize(responseLine) as Dictionary<string, object>;
                            return response;
                        }
                    }

                    // Check if process died
                    if (serverProcess.HasExited)
                    {
                        UnityEngine.Debug.LogError($"[GameSmith MCP] Server process exited unexpectedly with code {serverProcess.ExitCode}");
                        var remainingStderr = stderrReader.ReadToEnd();
                        if (!string.IsNullOrEmpty(remainingStderr))
                        {
                            UnityEngine.Debug.LogError($"[GameSmith MCP] Final stderr: {remainingStderr}");
                        }
                        return null;
                    }

                    System.Threading.Thread.Sleep(100);
                }

                UnityEngine.Debug.LogWarning($"[GameSmith MCP] Request timeout after 10 seconds ({checkCount} checks). Process still alive: {!serverProcess.HasExited}");

                // Try to read any pending stderr
                if (stderrReader.Peek() >= 0)
                {
                    var stderr = stderrReader.ReadToEnd();
                    UnityEngine.Debug.LogWarning($"[GameSmith MCP] Stderr after timeout: {stderr}");
                }

                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith MCP] Request failed: {ex.Message}\n{ex.StackTrace}");
                return null;
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

    /// <summary>
    /// Represents an MCP tool
    /// </summary>
    [Serializable]
    public class MCPTool
    {
        public string Name;
        public string Description;
        public object InputSchema;
    }
}
