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
        private Vector2 toolsScrollPosition = Vector2.zero;

        // Tool categories for color coding
        private enum ToolCategory
        {
            Creation,    // Green - create, add, generate
            Modification, // Blue - update, modify, set, move, rotate, scale
            Organization, // Orange - list, get, find, search, load
            Destructive  // Red - delete, remove, destroy, clear
        }

        [MenuItem("Tools/GameSmith/Configure Settings", false, 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSmithSettingsWindow>("GameSmith Settings");
            window.minSize = new Vector2(550, 450);
            window.maxSize = new Vector2(800, 1000);
            window.Show();
        }

        private void OnEnable()
        {
            config = GameSmithConfig.GetOrCreate();

            // Auto-detect Ollama models if Ollama is the active provider
            if (config != null && config.activeProvider.ToLower().Contains("ollama"))
            {
                config.RefreshOllamaModels();
            }
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

            // Set consistent label width for alignment
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120;

            // Scroll view with max width constraint
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Content container with padding and max width
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(position.width - 20));
            GUILayout.Space(15);

            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.margin = new RectOffset(10, 10, 0, 10);
            EditorGUILayout.LabelField("GameSmith Configuration", titleStyle);

            EditorGUILayout.Space(5);

            // Provider Selection
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            DrawProviderSection();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            // Restore original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private void DrawProviderSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Section header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 13;
            headerStyle.margin = new RectOffset(0, 0, 0, 8);
            EditorGUILayout.LabelField("AI Provider", headerStyle);
            EditorGUILayout.Space(4);

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

                // Auto-refresh Ollama models when switching to Ollama
                if (config.activeProvider.ToLower().Contains("ollama"))
                {
                    config.RefreshOllamaModels();
                }

                // Reset to first model when changing provider
                var providerModels = config.GetModelsList();
                if (providerModels.Count > 0)
                {
                    config.selectedModel = providerModels[0];
                }
            }

            // Model dropdown with Ollama refresh button
            var models = config.GetModelsList();

            // For Ollama, show refresh button next to model dropdown
            if (config.activeProvider.ToLower().Contains("ollama"))
            {
                EditorGUILayout.BeginHorizontal();

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
                    EditorGUILayout.LabelField("Model", "No models found");
                }

                // Refresh button for Ollama
                if (GUILayout.Button("🔄", GUILayout.Width(32), GUILayout.Height(20)))
                {
                    config.RefreshOllamaModels();
                    Repaint(); // Force window refresh to show new models
                }

                EditorGUILayout.EndHorizontal();

                if (models.Count == 0)
                {
                    var helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
                    helpBoxStyle.wordWrap = true;
                    EditorGUILayout.HelpBox("No Ollama models detected. Ensure Ollama is running with installed models.\n\nStart: ollama serve\nInstall: ollama pull codellama", MessageType.Warning);
                }
            }
            else
            {
                // Non-Ollama providers - regular model dropdown
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
                    if (GUILayout.Button(showPassword ? "🙈" : "👁️", GUILayout.Width(32), GUILayout.Height(20)))
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
                GUILayout.FlexibleSpace();

                // Left button: Get/Open API Key URL
                if (!string.IsNullOrEmpty(apiKeyUrl))
                {
                    if (GUILayout.Button("Get API Key", GUILayout.Height(26), GUILayout.MinWidth(120), GUILayout.MaxWidth(200)))
                    {
                        Application.OpenURL(apiKeyUrl);
                    }
                    GUILayout.Space(5);
                }

                // Right button: Dynamic based on state
                GUI.enabled = !isVerifying;

                if (isVerified)
                {
                    // State 3: Verified → Show "Edit API Key"
                    if (GUILayout.Button("Edit API Key", GUILayout.Height(26), GUILayout.MinWidth(120), GUILayout.MaxWidth(200)))
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
                    if (GUILayout.Button(isVerifying ? "Verifying..." : "Verify API Key", GUILayout.Height(26), GUILayout.MinWidth(120), GUILayout.MaxWidth(200)))
                    {
                        VerifyApiKeyAsync().Forget();
                    }
                }
                else if (!string.IsNullOrEmpty(currentKey))
                {
                    // State 1a: Invalid format → Show "Check Format"
                    if (GUILayout.Button("Check Format", GUILayout.Height(26), GUILayout.MinWidth(120), GUILayout.MaxWidth(200)))
                    {
                        ShowApiKeyFormatHelp();
                    }
                }

                GUILayout.FlexibleSpace();
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Ollama runs locally. No API key needed.\nMake sure Ollama is running: ollama serve", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            
            // Divider
            var dividerRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(dividerRect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(10);

            // Model Parameters section
            var paramHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            paramHeaderStyle.fontSize = 13;
            paramHeaderStyle.margin = new RectOffset(0, 0, 0, 8);
            EditorGUILayout.LabelField("Model Parameters", paramHeaderStyle);
            EditorGUILayout.Space(4);

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
            
            // Section header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 13;
            headerStyle.margin = new RectOffset(0, 0, 0, 8);
            EditorGUILayout.LabelField("Unity MCP Server", headerStyle);
            EditorGUILayout.Space(4);

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

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (isInstalled)
            {
                if (GUILayout.Button(isInstallingMCP ? "Updating..." : "Update MCP Server", GUILayout.Height(26), GUILayout.MinWidth(180), GUILayout.MaxWidth(250)))
                {
                    InstallOrUpdateMCPAsync().Forget();
                }
            }
            else
            {
                if (GUILayout.Button(isInstallingMCP ? "Installing..." : "Install MCP Server", GUILayout.Height(26), GUILayout.MinWidth(180), GUILayout.MaxWidth(250)))
                {
                    InstallOrUpdateMCPAsync().Forget();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            // Available Tools Section (collapsible)
            if (isInstalled)
            {
                EditorGUILayout.Space(10);
                
                // Divider before tools section
                var dividerRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(dividerRect, new Color(0.3f, 0.3f, 0.3f, 1f));
                EditorGUILayout.Space(8);

                EditorGUI.BeginChangeCheck();
                showMCPTools = EditorGUILayout.Foldout(showMCPTools, "Available Tools", true);
                if (EditorGUI.EndChangeCheck() && showMCPTools && cachedMCPTools == null && !isLoadingTools)
                {
                    // Load tools when foldout is opened for the first time
                    LoadMCPToolsAsync().Forget();
                }

                if (showMCPTools)
                {
                    EditorGUILayout.Space(6);
                    
                    if (isLoadingTools)
                    {
                        EditorGUILayout.LabelField("Loading tools...", EditorStyles.miniLabel);
                    }
                    else if (cachedMCPTools != null && cachedMCPTools.Count > 0)
                    {
                        // Header with count
                        EditorGUILayout.LabelField($"Found {cachedMCPTools.Count} tool(s)", EditorStyles.miniLabel);
                        EditorGUILayout.Space(8);

                        // Group and sort tools by category
                        var groupedTools = GroupToolsByCategory(cachedMCPTools);

                        // Scrollable list view
                        toolsScrollPosition = EditorGUILayout.BeginScrollView(toolsScrollPosition, 
                            GUILayout.Height(Mathf.Min(300, cachedMCPTools.Count * 20 + groupedTools.Count * 30 + 10)));

                        foreach (var group in groupedTools)
                        {
                            // Category header - simple and clean
                            var categoryColor = GetCategoryAccentColor(group.Key);
                            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
                            labelStyle.fontSize = 11;
                            labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                            
                            EditorGUILayout.BeginHorizontal();
                            // Colored indicator bar
                            var barRect = GUILayoutUtility.GetRect(3, 16, GUILayout.Width(3));
                            EditorGUI.DrawRect(barRect, categoryColor);
                            EditorGUILayout.LabelField(GetCategoryLabel(group.Key) + $" ({group.Value.Count})", labelStyle);
                            EditorGUILayout.EndHorizontal();
                            
                            EditorGUILayout.Space(4);
                            
                            // Tools in this category - simple list
                            foreach (var tool in group.Value)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(8); // Indent
                                var toolLabelStyle = new GUIStyle(EditorStyles.label);
                                toolLabelStyle.wordWrap = true;
                                EditorGUILayout.LabelField(tool.Name, toolLabelStyle, GUILayout.ExpandWidth(true));
                                EditorGUILayout.EndHorizontal();
                            }
                            
                            EditorGUILayout.Space(8);
                        }

                        EditorGUILayout.EndScrollView();
                        
                        // Legend - simple and clean
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField("Legend", EditorStyles.miniLabel);
                        EditorGUILayout.Space(4);
                        DrawCategoryLegend();
                    }
                    else if (cachedMCPTools != null && cachedMCPTools.Count == 0)
                    {
                        EditorGUILayout.LabelField("No tools available", EditorStyles.miniLabel);
                        if (GUILayout.Button("Refresh Tools", GUILayout.Height(24)))
                        {
                            LoadMCPToolsAsync().Forget();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Click to load tools", EditorStyles.miniLabel);
                        if (GUILayout.Button("Refresh Tools", GUILayout.Height(24)))
                        {
                            LoadMCPToolsAsync().Forget();
                        }
                    }
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
        /// Verify API key by making a test request (async)
        /// </summary>
        private async UniTaskVoid VerifyApiKeyAsync()
        {
            isVerifying = true;
            verificationMessage = "Verifying API key...";
            Repaint();

            try
            {
                var testClient = new AIAgentClient(config);
                bool completed = false;
                bool success = false;
                string errorMessage = "";

                // Send test request
                testClient.SendMessage(
                    "Hello",
                    "You are a test assistant. Reply with 'OK' only.",
                    null,
                    response =>
                    {
                        completed = true;
                        success = true;
                    },
                    error =>
                    {
                        completed = true;
                        success = false;
                        errorMessage = error;
                    }
                );

                // Wait for completion with timeout
                var cts = new CancellationTokenSource();
                cts.CancelAfterSlim(System.TimeSpan.FromSeconds(30));

                try
                {
                    await UniTask.WaitUntil(() => completed, cancellationToken: cts.Token);

                    if (success)
                    {
                        GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, true);
                        verificationMessage = $"✅ API key verified";
                    }
                    else
                    {
                        GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, false);
                        verificationMessage = $"❌ {errorMessage}";
                        GameSmithLogger.LogError(errorMessage);
                    }
                }
                catch (System.OperationCanceledException)
                {
                    verificationMessage = "❌ Timed out. Check your connection.";
                    GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, false);
                    GameSmithLogger.LogWarning("API key verification timed out");
                }
            }
            catch (System.Exception ex)
            {
                verificationMessage = $"❌ {ex.Message}";
                GameSmithSettings.Instance.SetApiKeyVerified(config.activeProvider, false);
                GameSmithLogger.LogError(ex.Message);
            }
            finally
            {
                isVerifying = false;
                Repaint();
            }
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
                // Check if there's already a connected MCP client from GameSmithWindow
                var connectedClient = GameSmithWindow.ConnectedMCPClient;
                if (connectedClient != null && connectedClient.IsConnected)
                {
                    // Use the existing connected client
                    cachedMCPTools = new System.Collections.Generic.List<MCPTool>(connectedClient.AvailableTools);
                    GameSmithLogger.Log($"Using existing MCP connection: {cachedMCPTools.Count} tools");
                    isLoadingTools = false;
                    Repaint();
                    return;
                }

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

        private System.Collections.Generic.Dictionary<ToolCategory, System.Collections.Generic.List<MCPTool>> GroupToolsByCategory(System.Collections.Generic.List<MCPTool> tools)
        {
            var grouped = new System.Collections.Generic.Dictionary<ToolCategory, System.Collections.Generic.List<MCPTool>>();
            
            // Initialize groups
            foreach (ToolCategory category in System.Enum.GetValues(typeof(ToolCategory)))
            {
                grouped[category] = new System.Collections.Generic.List<MCPTool>();
            }

            // Categorize tools based on name patterns
            foreach (var tool in tools)
            {
                var category = CategorizeToolByName(tool.Name);
                grouped[category].Add(tool);
            }

            // Sort tools within each category alphabetically
            foreach (var group in grouped.Values)
            {
                group.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
            }

            // Return only non-empty groups in order
            var result = new System.Collections.Generic.Dictionary<ToolCategory, System.Collections.Generic.List<MCPTool>>();
            var orderedCategories = new[] { ToolCategory.Creation, ToolCategory.Modification, ToolCategory.Organization, ToolCategory.Destructive };
            
            foreach (var category in orderedCategories)
            {
                if (grouped[category].Count > 0)
                {
                    result[category] = grouped[category];
                }
            }

            return result;
        }

        private ToolCategory CategorizeToolByName(string toolName)
        {
            var name = toolName.ToLowerInvariant();

            // Destructive actions (red)
            if (name.Contains("delete") || name.Contains("remove") || name.Contains("destroy") || 
                name.Contains("clear") || name.Contains("clean"))
            {
                return ToolCategory.Destructive;
            }

            // Creation actions (green)
            if (name.Contains("create") || name.Contains("add") || name.Contains("generate") || 
                name.Contains("spawn") || name.Contains("instantiate") || name.Contains("new") ||
                name.Contains("build") || name.Contains("make"))
            {
                return ToolCategory.Creation;
            }

            // Modification actions (blue)
            if (name.Contains("update") || name.Contains("modify") || name.Contains("set") || 
                name.Contains("move") || name.Contains("rotate") || name.Contains("scale") ||
                name.Contains("transform") || name.Contains("change") || name.Contains("edit") ||
                name.Contains("apply") || name.Contains("configure"))
            {
                return ToolCategory.Modification;
            }

            // Default to organization (orange) - list, get, find, search, load, etc.
            return ToolCategory.Organization;
        }

        private Color GetCategoryAccentColor(ToolCategory category)
        {
            switch (category)
            {
                case ToolCategory.Creation:
                    return new Color(0.3f, 0.75f, 0.4f, 1f); // Muted green
                case ToolCategory.Modification:
                    return new Color(0.35f, 0.6f, 0.9f, 1f); // Muted blue
                case ToolCategory.Organization:
                    return new Color(0.85f, 0.65f, 0.25f, 1f); // Muted orange
                case ToolCategory.Destructive:
                    return new Color(0.8f, 0.35f, 0.35f, 1f); // Muted red
                default:
                    return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        private string GetCategoryLabel(ToolCategory category)
        {
            switch (category)
            {
                case ToolCategory.Creation:
                    return "Creation";
                case ToolCategory.Modification:
                    return "Modification";
                case ToolCategory.Organization:
                    return "Information";
                case ToolCategory.Destructive:
                    return "Destructive";
                default:
                    return "Tools";
            }
        }

        private void DrawCategoryLegend()
        {
            EditorGUILayout.BeginHorizontal();
            
            var labelStyle = EditorStyles.miniLabel;
            
            // Creation
            EditorGUILayout.BeginHorizontal(GUILayout.Width(70));
            var createColor = GetCategoryAccentColor(ToolCategory.Creation);
            var createRect = GUILayoutUtility.GetRect(8, 8, GUILayout.Width(8), GUILayout.Height(8));
            EditorGUI.DrawRect(createRect, createColor);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Create", labelStyle);
            EditorGUILayout.EndHorizontal();
            
            // Modification
            EditorGUILayout.BeginHorizontal(GUILayout.Width(70));
            var modifyColor = GetCategoryAccentColor(ToolCategory.Modification);
            var modifyRect = GUILayoutUtility.GetRect(8, 8, GUILayout.Width(8), GUILayout.Height(8));
            EditorGUI.DrawRect(modifyRect, modifyColor);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Modify", labelStyle);
            EditorGUILayout.EndHorizontal();
            
            // Organization
            EditorGUILayout.BeginHorizontal(GUILayout.Width(60));
            var infoColor = GetCategoryAccentColor(ToolCategory.Organization);
            var infoRect = GUILayoutUtility.GetRect(8, 8, GUILayout.Width(8), GUILayout.Height(8));
            EditorGUI.DrawRect(infoRect, infoColor);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Info", labelStyle);
            EditorGUILayout.EndHorizontal();
            
            // Destructive
            EditorGUILayout.BeginHorizontal(GUILayout.Width(70));
            var deleteColor = GetCategoryAccentColor(ToolCategory.Destructive);
            var deleteRect = GUILayoutUtility.GetRect(8, 8, GUILayout.Width(8), GUILayout.Height(8));
            EditorGUI.DrawRect(deleteRect, deleteColor);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Delete", labelStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
