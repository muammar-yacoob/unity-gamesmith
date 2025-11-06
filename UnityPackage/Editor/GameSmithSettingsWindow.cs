using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple settings window for GameSmith configuration
    /// </summary>
    public class GameSmithSettingsWindow : EditorWindow
    {
        private GameSmithConfig config;
        private Vector2 scrollPosition;
        private bool isVerifying = false;
        private string verificationMessage = "";
        private bool showPassword = false;

        // MCP installation tracking
        private bool isInstallingMCP = false;
        private string mcpInstallStatus = "";
        private CancellationTokenSource mcpInstallCts;
        private bool showMCPTools = false;
        private System.Collections.Generic.List<MCPTool> cachedMCPTools = null;
        private bool isLoadingTools = false;

        [MenuItem("Tools/GameSmith/Configure Settings", false, 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSmithSettingsWindow>("GameSmith Settings");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnEnable()
        {
            config = GameSmithConfig.GetOrCreate();
        }

        private void OnDisable()
        {
            // Cancel any ongoing MCP installation
            if (mcpInstallCts != null)
            {
                mcpInstallCts.Cancel();
                mcpInstallCts.Dispose();
                mcpInstallCts = null;
            }
        }

        private void OnGUI()
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("Failed to load configuration.", MessageType.Error);
                if (GUILayout.Button("Reload")) OnEnable();
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("GameSmith Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Provider Selection
            DrawProviderSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawProviderSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("AI Provider", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Provider dropdown with status indicators
            var providerNames = config.GetProviderNames().ToArray();
            var providerNamesWithStatus = providerNames.Select(p => GetProviderStatusIndicator(p) + " " + p).ToArray();

            int currentProviderIndex = System.Array.IndexOf(providerNames, config.activeProvider);
            if (currentProviderIndex < 0 && providerNames.Length > 0)
            {
                currentProviderIndex = 0;
                config.activeProvider = providerNames[0];
            }

            EditorGUI.BeginChangeCheck();
            int newProviderIndex = EditorGUILayout.Popup("Provider", currentProviderIndex, providerNamesWithStatus);
            if (EditorGUI.EndChangeCheck() && newProviderIndex >= 0)
            {
                config.activeProvider = providerNames[newProviderIndex];
                // Reset to first model when changing provider
                var providerModels = config.GetModelsList();
                if (providerModels.Count > 0)
                {
                    config.selectedModel = providerModels[0];
                }
            }

            // Model dropdown
            var models = config.GetModelsList();
            if (models.Count > 0)
            {
                var modelDisplayNames = models.Select(m => config.GetModelDisplayName(m)).ToArray();
                int currentModelIndex = models.IndexOf(config.selectedModel);
                if (currentModelIndex < 0) currentModelIndex = 0;

                EditorGUI.BeginChangeCheck();
                int newModelIndex = EditorGUILayout.Popup("Model", currentModelIndex, modelDisplayNames);
                if (EditorGUI.EndChangeCheck() && newModelIndex >= 0)
                {
                    config.selectedModel = models[newModelIndex];
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No models available for this provider.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // API Key (not for Ollama)
            if (!config.activeProvider.ToLower().Contains("ollama"))
            {
                string currentKey = config.apiKey;
                bool isVerified = GameSmithSettings.Instance.IsApiKeyVerified(config.activeProvider);
                bool hasValidFormat = GameSmithSettings.ValidateApiKeyFormat(config.activeProvider, currentKey);

                // Show API key input field only if not verified
                if (!isVerified)
                {
                    // API Key field with show/hide toggle
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    string newKey;
                    if (showPassword)
                    {
                        newKey = EditorGUILayout.TextField("API Key", currentKey);
                    }
                    else
                    {
                        newKey = EditorGUILayout.PasswordField("API Key", currentKey);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        config.SetApiKey(config.activeProvider, newKey);
                        verificationMessage = ""; // Clear message when key changes
                    }

                    // Show/Hide toggle button
                    if (GUILayout.Button(showPassword ? "🙈" : "👁", GUILayout.Width(30), GUILayout.Height(18)))
                    {
                        showPassword = !showPassword;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Show API key status (single help box)
                if (!string.IsNullOrEmpty(verificationMessage))
                {
                    EditorGUILayout.HelpBox(verificationMessage, isVerified ? MessageType.Info : MessageType.Error);
                }
                else if (!string.IsNullOrEmpty(currentKey))
                {
                    if (isVerified)
                    {
                        EditorGUILayout.HelpBox("API key verified and working", MessageType.Info);
                    }
                    else if (hasValidFormat)
                    {
                        EditorGUILayout.HelpBox("API key format is valid but not verified yet", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("API key format appears invalid for " + config.activeProvider, MessageType.Error);
                    }
                }

                EditorGUILayout.Space(3);

                // Dynamic buttons based on state
                var apiKeyUrl = config.GetApiKeyUrl();

                EditorGUILayout.BeginHorizontal();

                // Left button: Get/Open API Key URL
                if (!string.IsNullOrEmpty(apiKeyUrl))
                {
                    if (GUILayout.Button("🔗 Get API Key", GUILayout.Height(28)))
                    {
                        Application.OpenURL(apiKeyUrl);
                    }
                }

                // Right button: Dynamic based on state
                GUI.enabled = !isVerifying;

                if (isVerified)
                {
                    // State 3: Verified → Show "Edit API Key"
                    if (GUILayout.Button("✏️ Edit API Key", GUILayout.Height(28)))
                    {
                        // Reset verification when user wants to edit
                        GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, false);
                        verificationMessage = "API key reset. Please verify again after editing.";
                        showPassword = false; // Reset to password mode when editing
                        Repaint();
                    }
                }
                else if (hasValidFormat && !string.IsNullOrEmpty(currentKey))
                {
                    // State 2: Valid format but not verified → Show "Verify API Key"
                    if (GUILayout.Button(isVerifying ? "⏳ Verifying..." : "✓ Verify API Key", GUILayout.Height(28)))
                    {
                        EditorCoroutineRunner.StartCoroutine(VerifyApiKey());
                    }
                }
                else if (!string.IsNullOrEmpty(currentKey))
                {
                    // State 1a: Invalid format → Show "Check Format"
                    if (GUILayout.Button("⚠️ Check Format", GUILayout.Height(28)))
                    {
                        ShowApiKeyFormatHelp();
                    }
                }

                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Ollama runs locally. No API key needed.\nMake sure Ollama is running: ollama serve", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Model Parameters
            EditorGUILayout.LabelField("Model Parameters", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.temperature = EditorGUILayout.Slider("Temperature", config.temperature, 0f, 2f);
            config.maxTokens = EditorGUILayout.IntSlider("Max Tokens", config.maxTokens, 256, 8192);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Unity MCP Server
            DrawMCPSection();
        }

        private void DrawMCPSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity MCP Server", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            string mcpVersion = GetMCPVersion();
            bool isInstalled = !string.IsNullOrEmpty(mcpVersion);

            if (isInstalled)
            {
                EditorGUILayout.LabelField("Status", "✓ Installed");
                EditorGUILayout.LabelField("Version", mcpVersion);
            }
            else
            {
                EditorGUILayout.HelpBox("Unity MCP Server not detected", MessageType.Warning);
            }

            // Show installation progress if running
            if (isInstallingMCP && !string.IsNullOrEmpty(mcpInstallStatus))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(mcpInstallStatus, MessageType.Info);
            }

            EditorGUILayout.Space(5);

            GUI.enabled = !isInstallingMCP;

            if (isInstalled)
            {
                if (GUILayout.Button(isInstallingMCP ? "⏳ Updating..." : "🔄 Update MCP Server", GUILayout.Height(28)))
                {
                    InstallOrUpdateMCPAsync().Forget();
                }
            }
            else
            {
                if (GUILayout.Button(isInstallingMCP ? "⏳ Installing..." : "⬇️ Install MCP Server", GUILayout.Height(28)))
                {
                    InstallOrUpdateMCPAsync().Forget();
                }
            }

            GUI.enabled = true;

            // Available Tools Section (collapsible)
            if (isInstalled)
            {
                EditorGUILayout.Space(5);

                EditorGUI.BeginChangeCheck();
                showMCPTools = EditorGUILayout.Foldout(showMCPTools, "Available Tools", true);
                if (EditorGUI.EndChangeCheck() && showMCPTools && cachedMCPTools == null && !isLoadingTools)
                {
                    // Load tools when foldout is opened for the first time
                    LoadMCPToolsAsync().Forget();
                }

                if (showMCPTools)
                {
                    EditorGUI.indentLevel++;

                    if (isLoadingTools)
                    {
                        EditorGUILayout.LabelField("Loading tools...", EditorStyles.miniLabel);
                    }
                    else if (cachedMCPTools != null && cachedMCPTools.Count > 0)
                    {
                        EditorGUILayout.LabelField($"Found {cachedMCPTools.Count} tool(s):", EditorStyles.miniLabel);
                        EditorGUILayout.Space(3);

                        foreach (var tool in cachedMCPTools)
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            EditorGUILayout.LabelField(tool.Name, EditorStyles.boldLabel);
                            if (!string.IsNullOrEmpty(tool.Description))
                            {
                                EditorGUILayout.LabelField(tool.Description, EditorStyles.wordWrappedMiniLabel);
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(2);
                        }
                    }
                    else if (cachedMCPTools != null && cachedMCPTools.Count == 0)
                    {
                        EditorGUILayout.LabelField("No tools available", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Click to load tools", EditorStyles.miniLabel);
                        if (GUILayout.Button("Refresh Tools", GUILayout.Height(20)))
                        {
                            LoadMCPToolsAsync().Forget();
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private string GetMCPVersion()
        {
            try
            {
                string appData = System.Environment.GetEnvironmentVariable("APPDATA");
                string packagePath = System.IO.Path.Combine(appData, "npm", "node_modules", "@spark-apps", "unity-mcp", "package.json");

                if (System.IO.File.Exists(packagePath))
                {
                    string json = System.IO.File.ReadAllText(packagePath);
                    // Simple JSON parsing for version
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                    if (versionMatch.Success)
                    {
                        return versionMatch.Groups[1].Value;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[GameSmith] Could not check MCP version: {ex.Message}");
            }

            return null;
        }

        private async UniTaskVoid InstallOrUpdateMCPAsync()
        {
            if (isInstallingMCP)
            {
                UnityEngine.Debug.LogWarning("[GameSmith] MCP installation already in progress");
                return;
            }

            isInstallingMCP = true;
            mcpInstallStatus = "Starting installation...";
            mcpInstallCts = new CancellationTokenSource();
            Repaint();

            try
            {
                await InstallMCPInternalAsync(mcpInstallCts.Token);
            }
            catch (System.Exception ex)
            {
                mcpInstallStatus = $"❌ Installation failed: {ex.Message}";
                GameSmithLogger.LogError($"MCP installation error: {ex.Message}");

                EditorUtility.DisplayDialog(
                    "Installation Error",
                    $"Failed to install Unity MCP Server:\n{ex.Message}\n\n" +
                    "Please manually run in Command Prompt:\nnpm install -g @spark-apps/unity-mcp",
                    "OK"
                );
            }
            finally
            {
                isInstallingMCP = false;
                mcpInstallCts?.Dispose();
                mcpInstallCts = null;
                Repaint();
            }
        }

        private async UniTask InstallMCPInternalAsync(CancellationToken cancellationToken)
        {
            // Switch to thread pool for process execution
            await UniTask.SwitchToThreadPool();

            Process npmProcess = null;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm install -g @spark-apps/unity-mcp",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                npmProcess = new Process { StartInfo = startInfo };

                await UniTask.SwitchToMainThread();
                GameSmithLogger.Log("Installing Unity MCP Server via npm...");
                mcpInstallStatus = "⏳ Running: npm install -g @spark-apps/unity-mcp";
                Repaint();
                await UniTask.SwitchToThreadPool();

                if (!npmProcess.Start())
                {
                    throw new System.Exception("Failed to start npm process");
                }

                // Read output asynchronously
                var outputLines = new System.Collections.Generic.List<string>();
                var errorLines = new System.Collections.Generic.List<string>();

                // Start reading stdout
                var readOutputTask = ReadProcessOutputAsync(npmProcess.StandardOutput, outputLines, cancellationToken);

                // Start reading stderr
                var readErrorTask = ReadProcessOutputAsync(npmProcess.StandardError, errorLines, cancellationToken);

                // Wait for process to complete with timeout (5 minutes)
                var processExitTask = UniTask.WaitUntil(() => npmProcess.HasExited, cancellationToken: cancellationToken);
                var timeoutTask = UniTask.Delay(System.TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);

                var winIndex = await UniTask.WhenAny(processExitTask, timeoutTask);

                if (winIndex != 0) // timeout occurred
                {
                    npmProcess.Kill();
                    throw new System.Exception("Installation timed out after 5 minutes");
                }

                // Wait a bit for output to finish
                await UniTask.Delay(500, cancellationToken: cancellationToken);

                // Check exit code
                if (npmProcess.ExitCode != 0)
                {
                    string errorOutput = string.Join("\n", errorLines);
                    throw new System.Exception($"npm exited with code {npmProcess.ExitCode}:\n{errorOutput}");
                }

                // Success!
                await UniTask.SwitchToMainThread();
                string newVersion = GetMCPVersion();
                mcpInstallStatus = $"✅ Installation successful! Version: {newVersion ?? "unknown"}";
                GameSmithLogger.Log($"Unity MCP Server installed successfully. Version: {newVersion}");

                EditorUtility.DisplayDialog(
                    "Installation Complete",
                    $"Unity MCP Server has been installed successfully!\n\nVersion: {newVersion ?? "unknown"}\n\n" +
                    "Please restart Unity for changes to take effect.",
                    "OK"
                );
            }
            catch (System.OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                mcpInstallStatus = "Installation cancelled";
                throw;
            }
            catch (System.Exception ex)
            {
                await UniTask.SwitchToMainThread();
                throw new System.Exception($"Installation failed: {ex.Message}", ex);
            }
            finally
            {
                npmProcess?.Dispose();
            }
        }

        private async UniTask ReadProcessOutputAsync(System.IO.StreamReader reader, System.Collections.Generic.List<string> lines, CancellationToken cancellationToken)
        {
            try
            {
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    string line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);

                        // Only show meaningful progress, filter out npm warnings
                        if (!line.Contains("npm WARN") && !line.Contains("npm notice"))
                        {
                            // Update status with latest line on main thread
                            await UniTask.SwitchToMainThread();

                            // Show more user-friendly messages
                            if (line.Contains("added") || line.Contains("changed") || line.Contains("removed"))
                            {
                                mcpInstallStatus = "⏳ Installing packages...";
                            }
                            else if (line.Trim().Length > 0 && line.Trim().Length < 100)
                            {
                                mcpInstallStatus = $"⏳ {line.Trim()}";
                            }

                            Repaint();
                            await UniTask.SwitchToThreadPool();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[GameSmith] Error reading process output: {ex.Message}");
            }
        }

        /// <summary>
        /// Verify API key by making a test request
        /// </summary>
        private IEnumerator VerifyApiKey()
        {
            isVerifying = true;
            verificationMessage = "Verifying API key...";
            Repaint();

            // Create a simple test request
            var testClient = new AIAgentClient(config);
            string errorMessage = "";

            testClient.SendMessage(
                "Hello",
                "You are a test assistant. Reply with 'OK' only.",
                null,
                response =>
                {
                    // Success!
                    GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, true);
                    verificationMessage = $"✅ API key verified successfully for {config.activeProvider}!";
                    GameSmithLogger.Log($"API key verified for {config.activeProvider}");
                },
                error =>
                {
                    // Failed
                    errorMessage = error;
                    GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, false);
                    verificationMessage = $"❌ Verification failed: {error}";
                    GameSmithLogger.LogWarning($"API key verification failed for {config.activeProvider}: {error}");
                }
            );

            // Wait for response (max 30 seconds)
            float timeout = 30f;
            float elapsed = 0f;

            while (string.IsNullOrEmpty(verificationMessage.Contains("✅") ? "done" : "") &&
                   string.IsNullOrEmpty(errorMessage) &&
                   elapsed < timeout)
            {
                yield return null;
                elapsed += 0.1f;

                // Check if verification completed
                if (verificationMessage.Contains("✅") || verificationMessage.Contains("❌"))
                {
                    break;
                }
            }

            if (elapsed >= timeout && !verificationMessage.Contains("✅") && !verificationMessage.Contains("❌"))
            {
                verificationMessage = "❌ Verification timed out. Please check your connection and try again.";
                GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, false);
            }

            isVerifying = false;
            Repaint();
        }

        /// <summary>
        /// Show API key format help dialog
        /// </summary>
        private void ShowApiKeyFormatHelp()
        {
            string formatHelp = "";

            switch (config.activeProvider)
            {
                case "Claude":
                    formatHelp = "Claude API keys should:\n" +
                                "• Start with: sk-ant-\n" +
                                "• Be longer than 20 characters\n" +
                                "• Example: sk-ant-api03-xxxxx...\n\n" +
                                "Get your key at:\nhttps://console.anthropic.com/settings/keys";
                    break;

                case "OpenAI":
                    formatHelp = "OpenAI API keys should:\n" +
                                "• Start with: sk-\n" +
                                "• Be longer than 20 characters\n" +
                                "• Example: sk-proj-xxxxx...\n\n" +
                                "Get your key at:\nhttps://platform.openai.com/api-keys";
                    break;

                case "Gemini":
                    formatHelp = "Gemini API keys should:\n" +
                                "• Start with: AIza\n" +
                                "• Be longer than 30 characters\n" +
                                "• Example: AIzaSyxxxxx...\n\n" +
                                "Get your key at:\nhttps://aistudio.google.com/app/apikey";
                    break;

                default:
                    formatHelp = $"{config.activeProvider} API keys should:\n" +
                                "• Be at least 20 characters long\n" +
                                "• Check the provider's documentation for the exact format";
                    break;
            }

            EditorUtility.DisplayDialog(
                $"{config.activeProvider} API Key Format",
                formatHelp,
                "OK"
            );
        }

        /// <summary>
        /// Get status indicator emoji for a provider
        /// 🟢 Green - Configured
        /// 🟠 Orange - Partially configured
        /// 🔴 Red - Not configured
        /// </summary>
        private string GetProviderStatusIndicator(string providerName)
        {
            // Get provider data from config
            var currentActive = config.activeProvider;

            // Temporarily switch to check this provider
            config.activeProvider = providerName;
            var providerData = config.GetActiveProviderData();
            string apiKey = config.apiKey;
            string apiUrl = providerData?.apiUrl ?? "";
            bool isVerified = GameSmithSettings.Instance.IsApiKeyVerified(providerName);

            // Restore original active provider
            config.activeProvider = currentActive;

            // Check if it's Ollama (no API key needed)
            bool isOllama = providerName.ToLower().Contains("ollama");

            // Determine status
            if (isOllama)
            {
                // Ollama just needs a valid URL
                if (!string.IsNullOrEmpty(apiUrl))
                {
                    return "🟢"; // Green - configured
                }
                else
                {
                    return "🔴"; // Red - no URL
                }
            }
            else
            {
                // Other providers need both URL and API key
                bool hasUrl = !string.IsNullOrEmpty(apiUrl);
                bool hasApiKey = !string.IsNullOrEmpty(apiKey);

                if (hasUrl && hasApiKey)
                {
                    return "🟢"; // Green - configured
                }
                else if (hasUrl || hasApiKey)
                {
                    return "🟠"; // Orange - partially configured
                }
                else
                {
                    return "🔴"; // Red - not configured
                }
            }
        }

        private async UniTaskVoid LoadMCPToolsAsync()
        {
            if (isLoadingTools) return;

            isLoadingTools = true;
            cachedMCPTools = null;
            Repaint();

            try
            {
                // Create a temporary MCP client to get tools list
                var tempClient = new MCPClientAsync();

                // Use npx.cmd on Windows
                string command = System.Environment.OSVersion.Platform == System.PlatformID.Win32NT ? "npx.cmd" : "npx";

                bool connected = false;
                tempClient.StartServerAsync(command, new[] { "@spark-apps/unity-mcp" }, success =>
                {
                    connected = success;
                });

                // Wait for connection (max 10 seconds)
                float timeout = 10f;
                float elapsed = 0f;
                while (!connected && elapsed < timeout)
                {
                    await UniTask.Delay(100);
                    elapsed += 0.1f;

                    if (tempClient.IsConnected)
                    {
                        connected = true;
                        break;
                    }
                }

                if (connected && tempClient.IsConnected)
                {
                    // Get tools list
                    cachedMCPTools = new System.Collections.Generic.List<MCPTool>(tempClient.AvailableTools);
                    GameSmithLogger.Log($"Loaded {cachedMCPTools.Count} MCP tools");
                }
                else
                {
                    cachedMCPTools = new System.Collections.Generic.List<MCPTool>();
                    GameSmithLogger.LogWarning("Failed to connect to MCP server to get tools list");
                }

                // Cleanup
                tempClient.Dispose();
            }
            catch (System.Exception ex)
            {
                cachedMCPTools = new System.Collections.Generic.List<MCPTool>();
                GameSmithLogger.LogError($"Error loading MCP tools: {ex.Message}");
            }
            finally
            {
                isLoadingTools = false;
                Repaint();
            }
        }
    }
}
