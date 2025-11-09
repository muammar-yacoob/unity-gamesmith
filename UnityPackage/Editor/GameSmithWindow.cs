using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Manipulator for resizing the divider between chat and input areas
    /// </summary>
    public class ResizeDividerManipulator : MouseManipulator
    {
        private VisualElement targetElement;
        private bool isDragging;
        private float startMouseY;
        private float startHeight;
        public System.Action OnResize;

        public ResizeDividerManipulator(VisualElement target)
        {
            targetElement = target;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                isDragging = true;
                startMouseY = evt.mousePosition.y;
                var height = targetElement.resolvedStyle.height;
                if (height <= 0 || float.IsNaN(height))
                {
                    height = 60f; // Very compact default - divider positioned high
                }
                startHeight = height;
                target.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isDragging && targetElement != null)
            {
                float deltaY = evt.mousePosition.y - startMouseY;
                float newHeight = startHeight - deltaY; // Inverted because dragging divider up increases input area
                
                // Clamp to reasonable bounds (reduced minimum for smaller default)
                newHeight = Mathf.Clamp(newHeight, 60f, 800f);
                
                targetElement.style.height = newHeight;
                targetElement.style.flexGrow = 0;
                targetElement.style.flexShrink = 0;
                
                // Trigger resize callback
                OnResize?.Invoke();
                
                evt.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (isDragging)
            {
                isDragging = false;
                target.ReleaseMouse();
                evt.StopPropagation();
            }
        }
    }

    /// <summary>
    /// Simple AI chat window for Unity development
    /// </summary>
    public class GameSmithWindow : EditorWindow
    {
        // MCP integration enabled
        private GameSmithConfig config;
        private ChatHistory history;
        private AIAgentClient client;
        private MCPClientAsync mcpClient;

        // Static access to the connected MCP client for other windows
        public static MCPClientAsync ConnectedMCPClient => Instance?.mcpClient;
        
        // Singleton instance for static access
        private static GameSmithWindow Instance;

        // UI Elements
        private TextField messageInput;
        private VisualElement messagesContainer;
        private ScrollView chatScroll;
        private PopupField<string> modelDropdown;
        private Label providerStatus;
        private Button sendButton;

        // Store last user message for retry functionality
        private string lastUserMessage = "";

        [MenuItem("Tools/GameSmith/GameSmith AI &g", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<GameSmithWindow>("GameSmith AI", true, typeof(SceneView));
        }

        // Note: MenuItem for Configure Settings is in GameSmithSettingsWindow.cs
        public static void OpenSettingsWindow()
        {
            GameSmithSettingsWindow.ShowWindow();
        }

        [MenuItem("Tools/GameSmith/Show Welcome Window", false, 3)]
        private static void ShowWelcome()
        {
            GameSmithWelcomeWindow.ShowWindow();
        }

        public void CreateGUI()
        {
            // Set singleton instance
            Instance = this;

            // Load config and history
            config = GameSmithConfig.GetOrCreate();
            history = ChatHistory.GetOrCreate();
            client = new AIAgentClient(config);

            // Auto-detect Ollama models if Ollama is the active provider
            if (config != null && config.activeProvider.ToLower().Contains("ollama"))
            {
                config.RefreshOllamaModels();
            }

            // Load UXML - try multiple possible paths
            string[] uiPaths = new string[]
            {
                "Assets/GameSmith/UnityPackage/Editor/GameSmithWindow.uxml",
                "Assets/UnityPackage/Editor/GameSmithWindow.uxml",
                "Packages/com.spark-games.unity-gamesmith/Editor/GameSmithWindow.uxml"
            };

            VisualTreeAsset visualTree = null;
            foreach (var path in uiPaths)
            {
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (visualTree != null) break;
            }

            if (visualTree == null)
            {
                var label = new Label("GameSmithWindow.uxml not found! Checked paths:\n" + string.Join("\n", uiPaths));
                label.style.paddingTop = 20;
                label.style.paddingLeft = 20;
                rootVisualElement.Add(label);
                return;
            }

            var root = visualTree.CloneTree();
            rootVisualElement.Add(root);

            // Load USS - try multiple possible paths
            string[] stylePaths = new string[]
            {
                "Assets/GameSmith/UnityPackage/Editor/GameSmithWindow.uss",
                "Assets/UnityPackage/Editor/GameSmithWindow.uss",
                "Packages/com.spark-games.unity-gamesmith/Editor/GameSmithWindow.uss"
            };

            StyleSheet styleSheet = null;
            foreach (var path in stylePaths)
            {
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet != null) break;
            }

            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            InitializeUI(root);
            LoadChatHistory();

            // Auto-start MCP server (don't block window creation)
            EditorApplication.delayCall += StartMCPServerAsync;

            // Enable config updates
            EditorApplication.update += CheckConfigChanges;
            
            // Update input height when window is resized
            root.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (messageInput != null)
                {
                    UpdateInputFieldHeight();
                }
            });
        }
        
        private void UpdateInputFieldHeight()
        {
            // CSS flexbox now handles layout automatically
            // This method is kept for potential future adjustments
            // The input-field-container flexes to fill available space
            // The input-footer stays fixed at 44px
        }

        private void StartMCPServerAsync()
        {
            // Update status label to show starting
            var mcpStatus = rootVisualElement?.Q<Label>("mcp-status");
            if (mcpStatus != null)
            {
                mcpStatus.text = "⚒️ MCP: Starting...";
                mcpStatus.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
                mcpStatus.RemoveFromClassList("mcp-status-success");
                mcpStatus.RemoveFromClassList("mcp-status-error");
            }

            // Check if MCP server is already running
            if (mcpClient != null && mcpClient.IsConnected)
            {
                int toolCount = mcpClient.AvailableTools?.Count ?? 0;
                UnityEngine.Debug.Log($"[GameSmith] MCP already running with {toolCount} tools");

                // Update status to show tools count
                if (mcpStatus != null && toolCount > 0)
                {
                    mcpStatus.text = $"⚒️ MCP: {toolCount} tools";
                    mcpStatus.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
                    mcpStatus.AddToClassList("mcp-status-success");
                    return;
                }
                // If connected but no tools, fall through to restart
                UnityEngine.Debug.LogWarning("[GameSmith] MCP connected but no tools loaded. Restarting...");
            }

            UnityEngine.Debug.Log("[GameSmith] Starting MCP server...");
            AddMessageBubble("Starting MCP server...", false);

            if (mcpClient == null)
            {
                mcpClient = new MCPClientAsync();
            }

            // Try multiple paths to find unity-mcp package
            string packagePath = null;
            string[] possiblePaths = new string[]
            {
                // 1. Local installation in project (most reliable)
                System.IO.Path.Combine(Application.dataPath, "..", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"),

                // 2. Windows global installation
                System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("APPDATA") ?? "", "npm", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"),

                // 3. Unix/Linux/WSL global installation
                System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("HOME") ?? "", ".npm-global", "lib", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js"),

                // 4. Alternative Unix global installation
                "/usr/local/lib/node_modules/@spark-apps/unity-mcp/dist/index.js"
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    packagePath = path;
                    UnityEngine.Debug.Log($"[GameSmith] ✓ Found unity-mcp at: {packagePath}");
                    break;
                }
            }

            if (string.IsNullOrEmpty(packagePath))
            {
                UnityEngine.Debug.LogError("[GameSmith] unity-mcp not found in any expected location");
                UnityEngine.Debug.LogError("[GameSmith] Tried paths:");
                foreach (var path in possiblePaths)
                {
                    if (!string.IsNullOrEmpty(path))
                        UnityEngine.Debug.LogError($"  ✗ {path}");
                }
                UnityEngine.Debug.LogError("[GameSmith] Please install:");
                UnityEngine.Debug.LogError("  Global: npm install -g @spark-apps/unity-mcp");
                UnityEngine.Debug.LogError("  Local:  npm install @spark-apps/unity-mcp");
                AddMessageBubble("❌ MCP not installed. See console for details.", false);

                // Update UI status
                if (mcpStatus != null)
                {
                    mcpStatus.text = "⚒️ MCP: Not installed";
                    mcpStatus.style.color = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
                    mcpStatus.AddToClassList("mcp-status-error");
                }
                return;
            }

            string nodePath = "node";
            string[] args = new string[] { packagePath };

            UnityEngine.Debug.Log($"[GameSmith] Launching: {nodePath} {packagePath}");

            // Start server without blocking
            mcpClient.StartServerAsync(nodePath, args, (success) =>
            {
                var mcpStatus = rootVisualElement?.Q<Label>("mcp-status");
                int toolCount = mcpClient.AvailableTools?.Count ?? 0;

                if (success && toolCount > 0)
                {
                    var mcpUrl = "https://github.com/muammar-yacoob/unity-mcp";
                    UnityEngine.Debug.Log($"[GameSmith] ✓ MCP ready with {toolCount} tools. Visit {mcpUrl}");
                    AddMessageBubble($"✓ MCP ready ({toolCount} tools)", false);
                    if (mcpStatus != null)
                    {
                        mcpStatus.text = $"⚒️ MCP: {toolCount} tools";
                        mcpStatus.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
                        mcpStatus.RemoveFromClassList("mcp-status-error");
                        mcpStatus.AddToClassList("mcp-status-success");
                    }
                }
                else
                {
                    string reason = !success ? "Server failed to start" :
                                   toolCount == 0 ? "No tools loaded" : "Unknown error";
                    UnityEngine.Debug.LogError($"[GameSmith] ✗ MCP startup failed: {reason}");
                    UnityEngine.Debug.LogError($"[GameSmith] IsConnected: {mcpClient.IsConnected}, Tools: {toolCount}");
                    AddMessageBubble($"❌ MCP failed: {reason}. Check Unity console.", false);
                    if (mcpStatus != null)
                    {
                        mcpStatus.text = $"⚒️ MCP: {reason}";
                        mcpStatus.style.color = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
                        mcpStatus.RemoveFromClassList("mcp-status-success");
                        mcpStatus.AddToClassList("mcp-status-error");
                    }
                }
            });
        }

        // OLD MCP server startup code - REMOVED
        // This code was causing Unity to freeze due to blocking operations:
        // - Process.WaitForExit() blocking calls
        // - Synchronous npm install operations
        // - MCPClient.StartServer() with Thread.Sleep
        // Replaced with StartMCPServerAsync() using coroutines

        private void OnDestroy()
        {
            // Unregister all callbacks
            EditorApplication.update -= CheckConfigChanges;

            // Dispose MCP client
            mcpClient?.Dispose();

            // Clear references
            modelDropdown = null;
            messageInput = null;
            messagesContainer = null;
            chatScroll = null;
            providerStatus = null;
            sendButton = null;
        }

        private string lastConfigState = "";

        private void CheckConfigChanges()
        {
            if (config == null || modelDropdown == null) return;

            // Create a simple state string to detect changes
            var currentState = $"{config.apiUrl}|{config.apiKey}|{config.selectedModel}";

            if (currentState != lastConfigState)
            {
                lastConfigState = currentState;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            // Refresh model dropdown
            if (modelDropdown != null && config != null)
            {
                var models = config.GetModelsList();
                modelDropdown.choices = models;
                modelDropdown.SetValueWithoutNotify(config.GetCurrentModel());
            }

            // Update provider status
            UpdateProviderStatus();

            // Recreate client with new config
            client = new AIAgentClient(config);
        }

        private void InitializeUI(VisualElement root)
        {
            // Get UI elements
            messageInput = root.Q<TextField>("message-input");
            messagesContainer = root.Q<VisualElement>("messages-container");
            chatScroll = root.Q<ScrollView>("chat-scroll");
            providerStatus = root.Q<Label>("provider-status");
            sendButton = root.Q<Button>("send-button");

            // Get header element and add View Logs button
            var header = root.Q<VisualElement>("header");
            if (header == null)
            {
                header = root.Q<VisualElement>(className: "header");
            }

            // Find or get MCP status label (already in UXML)
            var mcpStatus = root.Q<Label>("mcp-status");
            if (mcpStatus != null)
            {
                mcpStatus.AddToClassList("mcp-status");
            }

            // Setup resizable divider
            var resizeDivider = root.Q<VisualElement>("resize-divider");
            var inputArea = root.Q<VisualElement>("input-area");
            if (resizeDivider != null && inputArea != null)
            {
                // Set initial height immediately to control space division
                inputArea.style.height = 60; // Very compact - divider positioned high
                inputArea.style.flexGrow = 0;
                inputArea.style.flexShrink = 0;
                
                var manipulator = new ResizeDividerManipulator(inputArea);
                manipulator.OnResize += UpdateInputFieldHeight;
                resizeDivider.AddManipulator(manipulator);
            }

            // Create model dropdown programmatically
            var modelDropdownContainer = root.Q<VisualElement>("model-dropdown-container");
            if (modelDropdownContainer != null)
            {
                // Clear any existing children
                modelDropdownContainer.Clear();

                var models = config.GetModelsList();
                var currentModel = config.GetCurrentModel();

                // Ensure current model is in the list, otherwise use first model
                if (models != null && models.Count > 0)
                {
                    if (!models.Contains(currentModel))
                    {
                        currentModel = models[0];
                        config.selectedModel = currentModel;
                    }

                    modelDropdown = new PopupField<string>(models, currentModel);
                    // Display only model name when closed (without details in parentheses)
                    // Full details shown when dropdown is open
                    modelDropdown.formatSelectedValueCallback = (val) => GetModelNameOnly(val);
                    modelDropdown.formatListItemCallback = (val) => config.GetModelDisplayName(val);
                    modelDropdown.RegisterValueChangedCallback(evt => OnModelChanged(evt.newValue));
                    modelDropdownContainer.Add(modelDropdown);
                }
                else
                {
                    // No models available - show label instead
                    var noModelsLabel = new Label("No models available");
                    noModelsLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                    modelDropdownContainer.Add(noModelsLabel);
                }
            }

            UpdateProviderStatus();

            // Make provider status clickable
            if (providerStatus != null)
            {
                providerStatus.RegisterCallback<ClickEvent>(OnProviderStatusClicked);
                providerStatus.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor() { texture = null });
            }

            // Setup button callbacks
            var settingsButton = root.Q<Button>("settings-button");
            if (settingsButton != null)
            {
                settingsButton.clicked += () => GameSmithSettingsWindow.ShowWindow();
            }

            var clearButton = root.Q<Button>("clear-button");
            if (clearButton != null)
            {
                clearButton.clicked += ClearChat;
            }

            // Add View Logs button to header (icon-only)
            if (header != null)
            {
                var viewLogsButton = new Button(() => GameSmithLogger.OpenLogFolder());
                viewLogsButton.text = "📝";
                viewLogsButton.tooltip = "View Logs";
                viewLogsButton.AddToClassList("icon-button");
                header.Add(viewLogsButton);
            }

            if (sendButton != null)
            {
                sendButton.clicked += SendMessage;
            }

            // Enter to send (Return and KeypadEnter), Shift+Enter inserts newline
            if (messageInput != null)
            {
                // Ensure multiline is enabled and field expands properly
                messageInput.multiline = true;
                messageInput.style.flexGrow = 1;
                messageInput.style.flexShrink = 1;
                
                // Update height initially and on resize
                EditorApplication.delayCall += UpdateInputFieldHeight;
                EditorApplication.delayCall += () => EditorApplication.delayCall += UpdateInputFieldHeight;
                
                // Register for geometry changes
                messageInput.RegisterCallback<GeometryChangedEvent>(evt => UpdateInputFieldHeight());
                
                // Also update when input area is resized
                var inputAreaElement = root.Q<VisualElement>("input-area");
                if (inputAreaElement != null)
                {
                    inputAreaElement.RegisterCallback<GeometryChangedEvent>(evt => UpdateInputFieldHeight());
                }
                
                // Update when footer changes size
                var inputFooterElement = root.Q<VisualElement>(className: "input-footer");
                if (inputFooterElement != null)
                {
                    inputFooterElement.RegisterCallback<GeometryChangedEvent>(evt => UpdateInputFieldHeight());
                }
                
                messageInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (!evt.shiftKey && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                    {
                        evt.StopPropagation();
                        SendMessage();
                    }
                }, TrickleDown.TrickleDown);
            }
        }

        private string GetModelNameOnly(string modelId)
        {
            var fullName = config.GetModelDisplayName(modelId);
            // Extract model name without details in parentheses
            var parenIndex = fullName.IndexOf('(');
            if (parenIndex > 0)
            {
                return fullName.Substring(0, parenIndex).Trim();
            }
            return fullName;
        }

        private void OnModelChanged(string newModel)
        {
            config.selectedModel = newModel;
            UpdateProviderStatus();
        }

        private void UpdateProviderStatus()
        {
            if (providerStatus == null || config == null) return;

            if (!config.IsValid())
            {
                providerStatus.text = "⚠ Not configured - Click to configure";
                providerStatus.style.color = new StyleColor(new Color(0.9f, 0.6f, 0.4f));
            }
            else
            {
                providerStatus.text = $"✓ {config.activeProvider}";
                providerStatus.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
            }
        }

        private void OnProviderStatusClicked(ClickEvent evt)
        {
            // Always open settings window on click
            GameSmithSettingsWindow.ShowWindow();
        }

        private void LoadChatHistory()
        {
            if (history == null || history.Messages == null) return;

            foreach (var message in history.Messages)
            {
                if (message.role == ChatMessage.Role.User)
                {
                    AddMessageBubble(message.content, true);
                }
                else if (message.role == ChatMessage.Role.Assistant)
                {
                    AddMessageBubble(message.content, false);
                }
            }
        }

        private bool IsRetryCommand(string messageLower)
        {
            // Detect common retry/continue phrases
            return messageLower == "try again" ||
                   messageLower == "retry" ||
                   messageLower == "continue" ||
                   messageLower == "again" ||
                   messageLower == "resend" ||
                   messageLower == "repeat";
        }

        private void SendMessage()
        {
            var message = messageInput.value;
            if (string.IsNullOrWhiteSpace(message)) return;

            // Check for retry/continue commands
            var messageLower = message.Trim().ToLower();
            if (IsRetryCommand(messageLower))
            {
                if (!string.IsNullOrEmpty(lastUserMessage))
                {
                    // Show retry indicator
                    AddMessageBubble("🔄 Retrying last message...", false);

                    // Use the last message instead
                    message = lastUserMessage;
                }
                else
                {
                    // No previous message to retry
                    AddMessageBubble("No previous message to retry.", false);
                    messageInput.value = "";
                    return;
                }
            }
            else
            {
                // Store this as the last user message for future retries
                lastUserMessage = message;
            }

            // Add user message (unless it was a retry command, then we already showed the retry indicator)
            if (!IsRetryCommand(messageLower))
            {
                AddMessageBubble(message, true);
                history.AddMessage(ChatMessage.Role.User, message);
            }

            // Clear input and disable during processing
            messageInput.value = "";
            messageInput.SetEnabled(false);
            if (sendButton != null)
            {
                sendButton.SetEnabled(false);
            }

            // Classify user intent to determine execution strategy
            var intent = IntentClassifier.Classify(message);

            // STRATEGY 1: Direct MCP Execution (no AI needed)
            if (intent.Type == IntentClassifier.IntentType.DirectMCP)
            {
                // Check MCP availability
                if (mcpClient == null || (!mcpClient.IsConnected && (mcpClient.AvailableTools == null || mcpClient.AvailableTools.Count == 0)))
                {
                    AddMessageBubble("❌ MCP server not ready. Restarting...", false);

                    // Try to restart MCP server
                    StartMCPServerAsync();

                    // Re-enable input
                    messageInput.SetEnabled(true);
                    if (sendButton != null) sendButton.SetEnabled(true);
                    messageInput.Focus();
                    return;
                }

                // Show what we're executing
                var executionDesc = IntentClassifier.GetExecutionDescription(intent);
                if (!string.IsNullOrEmpty(executionDesc))
                {
                    AddMessageBubble(executionDesc, false);
                }

                // Execute MCP tool directly - NO AI CALL
                UnityEngine.Debug.Log($"[GameSmith] Calling MCP tool: {intent.ToolName} with args: {MiniJSON.Json.Serialize(intent.Arguments)}");
                mcpClient.CallToolAsync(intent.ToolName, intent.Arguments, (toolResult) =>
                {
                    UnityEngine.Debug.Log($"[GameSmith] Tool result: {toolResult}");
                    // Format and display the result
                    string formattedResult = FormatToolResult(intent.ToolName, toolResult);
                    AddMessageBubble(formattedResult, false);
                    history.AddMessage(ChatMessage.Role.Assistant, formattedResult);

                    // Re-enable input
                    messageInput.SetEnabled(true);
                    if (sendButton != null) sendButton.SetEnabled(true);
                    messageInput.Focus();
                });
                return;
            }

            // STRATEGY 2 & 3: AI Required or Ambiguous - Send to AI with tools
            var tools = null as List<MCPTool>;
            if (mcpClient != null && mcpClient.AvailableTools != null && mcpClient.AvailableTools.Count > 0)
            {
                tools = mcpClient.AvailableTools;
            }

            // Build system context based on tool availability
            string systemContext;
            if (tools != null && tools.Count > 0)
            {
                systemContext = @"You are a Unity AI assistant with real-time access to the Unity Editor via MCP tools.

CRITICAL RULES:
1. The Unity MCP server IS RUNNING and tools ARE AVAILABLE - never say otherwise
2. When asked about scene objects, hierarchy, or Unity data: USE THE TOOLS IMMEDIATELY
3. Do not provide manual code or explanations - execute the appropriate tool
4. Tools have been verified and are ready to use

Example: ""list objects"" → Call unity_get_hierarchy tool right now

" + UnityProjectContext.GetProjectContext();
            }
            else
            {
                systemContext = @"You are a Unity AI assistant helping with game development.

Focus on providing clear, actionable advice for Unity development. When asked to write code, provide complete, working examples.

" + UnityProjectContext.GetProjectContext();
            }

            // Send to AI
            client.SendMessage(message, systemContext, tools,
                onSuccess: (response) => HandleAIResponse(response),
                onError: (error) => HandleError(error));
        }

        private void HandleAIResponse(AIResponse response)
        {
            // Display thinking content in a separate bubble if present
            if (!string.IsNullOrEmpty(response.ThinkingContent) && !string.IsNullOrWhiteSpace(response.ThinkingContent))
            {
                AddThinkingBubble(response.ThinkingContent);
            }

            // Display text content if any (but not if it's a tool use)
            if (!string.IsNullOrEmpty(response.TextContent) && !response.HasToolUse)
            {
                AddMessageBubble(response.TextContent, false);
                history.AddMessage(ChatMessage.Role.Assistant, response.TextContent);
            }

            // Handle tool use
            if (response.HasToolUse)
            {
                // Execute tool via MCP
                if (mcpClient != null && (mcpClient.IsConnected || (mcpClient.AvailableTools != null && mcpClient.AvailableTools.Count > 0)))
                {
                    mcpClient.CallToolAsync(response.ToolName, response.ToolInput, (toolResult) =>
                    {
                        // For Ollama, OpenAI, and Grok, display result and re-enable input
                        if (client.ActiveProvider.Contains("Ollama") || client.ActiveProvider.Contains("OpenAI") || client.ActiveProvider.Contains("Grok"))
                        {
                            // Parse and format the tool result for display
                            string formattedResult = FormatToolResult(response.ToolName, toolResult);
                            AddMessageBubble(formattedResult, false);
                            history.AddMessage(ChatMessage.Role.Assistant, formattedResult);

                            // Re-enable input for next message
                            messageInput.SetEnabled(true);
                            if (sendButton != null)
                            {
                                sendButton.SetEnabled(true);
                            }
                            messageInput.Focus();
                        }
                        else
                        {
                            // For Anthropic/Gemini, continue conversation with tool result
                            var continuationTools = mcpClient.AvailableTools;
                            var continuationContext = @"You are a Unity AI assistant with access to Unity scene manipulation tools via MCP.

The tool has been executed. Review the result and provide a clear, concise summary to the user.

If the result indicates success, acknowledge it briefly. If there's an error or the result needs clarification, explain it clearly.

" + UnityProjectContext.GetProjectContext();

                            // Send tool result back to AI to continue conversation
                            client.SendToolResult(response.ToolUseId, toolResult, continuationContext, continuationTools,
                                onSuccess: (nextResponse) => HandleAIResponse(nextResponse),
                                onError: (error) => HandleError(error));
                        }
                    });
                }
                else
                {
                    HandleError("MCP client not available for tool execution");
                }
            }
            else
            {
                // No more tool use, re-enable input
                messageInput.SetEnabled(true);
                if (sendButton != null)
                {
                    sendButton.SetEnabled(true);
                }
                messageInput.Focus();
            }
        }

        private string FormatToolResult(string toolName, string toolResult)
        {
            // Parse the tool result and format it nicely
            try
            {
                // Try to parse as JSON for better formatting
                var resultObj = MiniJSON.Json.Deserialize(toolResult) as Dictionary<string, object>;
                if (resultObj != null)
                {
                    // Format based on tool type
                    if (toolName == "unity_get_hierarchy")
                    {
                        if (resultObj.ContainsKey("objects"))
                        {
                            var objects = resultObj["objects"] as List<object>;
                            if (objects == null || objects.Count == 0)
                            {
                                return "The scene is empty.";
                            }
                            var objectNames = objects.Select(o =>
                            {
                                // Handle both string and dictionary formats
                                if (o is Dictionary<string, object> objDict && objDict.ContainsKey("name"))
                                    return objDict["name"].ToString();
                                return o.ToString();
                            }).ToArray();
                            return $"Objects in the scene:\n• " + string.Join("\n• ", objectNames);
                        }
                        else
                        {
                            // Maybe it's a direct array result
                            return "Scene hierarchy retrieved";
                        }
                    }
                    else if (toolName.Contains("create"))
                    {
                        var objName = resultObj.ContainsKey("name") ? resultObj["name"].ToString() : "object";
                        return $"✅ Created {objName} successfully";
                    }
                    else if (toolName.Contains("delete") || toolName.Contains("remove"))
                    {
                        return $"✅ Deleted object successfully";
                    }
                    else if (toolName.Contains("scale") || toolName.Contains("rotate") || toolName.Contains("translate") || toolName.Contains("move"))
                    {
                        return $"✅ Transform applied successfully";
                    }

                    // Generic format for other results
                    if (resultObj.ContainsKey("result"))
                    {
                        return resultObj["result"].ToString();
                    }
                    if (resultObj.ContainsKey("message"))
                    {
                        return resultObj["message"].ToString();
                    }
                }
            }
            catch
            {
                // If not JSON, just return the raw result cleaned up
            }

            // Fallback: clean up the result before returning
            if (string.IsNullOrEmpty(toolResult))
            {
                return $"✅ {toolName} executed successfully";
            }

            // Remove markdown code block markers if present
            string cleanedResult = toolResult;
            if (cleanedResult.StartsWith("```json") || cleanedResult.StartsWith("```"))
            {
                // Remove opening and closing code block markers
                cleanedResult = System.Text.RegularExpressions.Regex.Replace(cleanedResult, @"^```[^\n]*\n?", "");
                cleanedResult = System.Text.RegularExpressions.Regex.Replace(cleanedResult, @"\n?```$", "");

                // Try to parse the cleaned JSON
                try
                {
                    var obj = MiniJSON.Json.Deserialize(cleanedResult) as Dictionary<string, object>;
                    if (obj != null)
                    {
                        // Check for common response patterns
                        if (obj.ContainsKey("success") && (bool)obj["success"])
                        {
                            return $"✅ {toolName} completed successfully";
                        }
                        if (obj.ContainsKey("error"))
                        {
                            return $"❌ Error: {obj["error"]}";
                        }
                        // If it's a small object, format it nicely
                        if (obj.Count <= 3)
                        {
                            var parts = obj.Select(kvp => $"{kvp.Key}: {kvp.Value}");
                            return string.Join("\n", parts);
                        }
                    }
                }
                catch { }
            }

            // If result is too long, truncate it
            if (cleanedResult.Length > 200)
            {
                cleanedResult = cleanedResult.Substring(0, 197) + "...";
            }

            return cleanedResult;
        }

        private void HandleError(string error)
        {
            AddErrorBubble($"Error: {error}");

            // Re-enable input
            messageInput.SetEnabled(true);
            if (sendButton != null)
            {
                sendButton.SetEnabled(true);
            }
            messageInput.Focus();
        }

        private void AddMessageBubble(string text, bool isUser)
        {
            if (messagesContainer == null)
            {
                UnityEngine.Debug.LogWarning("[GameSmith] Cannot add message bubble - messagesContainer is null");
                return;
            }

            var bubble = new VisualElement();
            bubble.AddToClassList(isUser ? "message-user" : "message-assistant");

            var label = new Label(text);
            label.AddToClassList(isUser ? "message-user-text" : "message-assistant-text");

            bubble.Add(label);
            messagesContainer.Add(bubble);

            // Scroll to bottom
            EditorApplication.delayCall += () =>
            {
                if (chatScroll != null && chatScroll.contentContainer != null)
                {
                    chatScroll.scrollOffset = new Vector2(0, chatScroll.contentContainer.layout.height);
                }
            };
        }

        private void AddThinkingBubble(string thinkingContent)
        {
            if (messagesContainer == null || string.IsNullOrWhiteSpace(thinkingContent))
            {
                return;
            }

            var bubble = new VisualElement();
            bubble.AddToClassList("message-thinking");

            var thinkingFoldout = new UnityEngine.UIElements.Foldout();

            // Calculate thinking time (approximate based on content length)
            float thinkingTime = thinkingContent.Length * 0.01f;
            string timeText = thinkingTime < 1f ? $"{thinkingTime:F1}s" : $"{(int)thinkingTime}s";

            thinkingFoldout.text = $"💭 Thinking... ({timeText})";
            thinkingFoldout.value = false; // Collapsed by default
            thinkingFoldout.AddToClassList("thinking-foldout");

            var thinkingContentContainer = new VisualElement();
            thinkingContentContainer.AddToClassList("thinking-content");

            var thinkingLabel = new Label(thinkingContent);
            thinkingLabel.AddToClassList("thinking-text");
            thinkingContentContainer.Add(thinkingLabel);

            thinkingFoldout.Add(thinkingContentContainer);
            bubble.Add(thinkingFoldout);

            messagesContainer.Add(bubble);

            // Scroll to bottom
            EditorApplication.delayCall += () =>
            {
                if (chatScroll != null && chatScroll.contentContainer != null)
                {
                    chatScroll.scrollOffset = new Vector2(0, chatScroll.contentContainer.layout.height);
                }
            };
        }

        private void AddErrorBubble(string text)
        {
            var bubble = new VisualElement();
            bubble.AddToClassList("message-error");

            // Make clickable with pointer cursor
            bubble.style.cursor = new StyleCursor(StyleKeyword.Auto);
            bubble.RegisterCallback<ClickEvent>(evt =>
            {
                GameSmithSettingsWindow.ShowWindow();
            });

            var label = new Label(text + "\n\n💡 Click here to configure API settings");
            label.AddToClassList("message-error-text");

            bubble.Add(label);
            messagesContainer.Add(bubble);

            EditorApplication.delayCall += () =>
            {
                chatScroll.scrollOffset = new Vector2(0, chatScroll.contentContainer.layout.height);
            };
        }

        private void ClearChat()
        {
            if (EditorUtility.DisplayDialog("Clear Chat", "Clear all chat history?", "Yes", "No"))
            {
                history.ClearHistory();
                client?.ClearHistory();

                // Remove all message bubbles (keep welcome message)
                var messagesToRemove = messagesContainer.Query<VisualElement>()
                    .Where(e => e.ClassListContains("message-user") ||
                               e.ClassListContains("message-assistant") ||
                               e.ClassListContains("message-error"))
                    .ToList();

                foreach (var msg in messagesToRemove)
                {
                    messagesContainer.Remove(msg);
                }
            }
        }
    }
}
