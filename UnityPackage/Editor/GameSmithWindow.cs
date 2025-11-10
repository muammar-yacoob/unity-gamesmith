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

                // Calculate max height based on window size to keep footer visible
                // Reserve space for header (~64px), footer (~40px), chat messages (min 150px), and divider (4px)
                float windowHeight = targetElement.parent?.resolvedStyle.height ?? 600f;
                float maxAllowedHeight = windowHeight - 64f - 40f - 150f - 4f; // Leave room for other elements

                // Clamp to reasonable bounds
                newHeight = Mathf.Clamp(newHeight, 60f, Mathf.Min(maxAllowedHeight, 500f));

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

        // Static access to session token counts
        public static int SessionInputTokens => Instance?.sessionInputTokens ?? 0;
        public static int SessionOutputTokens => Instance?.sessionOutputTokens ?? 0;
        public static int SessionTotalTokens => SessionInputTokens + SessionOutputTokens;

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

        // Processing indicator bubble
        private VisualElement processingBubble = null;

        // Token usage tracking for session
        private int sessionInputTokens = 0;
        private int sessionOutputTokens = 0;

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

            // Clear system prompt cache to always load latest
            SystemPrompts.ClearCache();

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

            UnityEngine.Debug.Log($"[GameSmith] Starting MCP server: {MCPServerConfig.DisplayName}");
            AddMessageBubble($"Starting MCP server: {MCPServerConfig.DisplayName}...", false);

            if (mcpClient == null)
            {
                mcpClient = new MCPClientAsync();
            }

            // Use centralized MCP server configuration
            string command = MCPServerConfig.ServerCommand;
            string[] args = MCPServerConfig.GetServerArgs();

            UnityEngine.Debug.Log($"[GameSmith] Launching: {command} {string.Join(" ", args)}");

            // Start server without blocking
            mcpClient.StartServerAsync(command, args, (success) =>
            {
                var mcpStatus = rootVisualElement?.Q<Label>("mcp-status");
                int toolCount = mcpClient.AvailableTools?.Count ?? 0;

                if (success && toolCount > 0)
                {
                    UnityEngine.Debug.Log($"[GameSmith] ✓ MCP ready with {toolCount} tools ({MCPServerConfig.DisplayName})");
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
                    UnityEngine.Debug.LogError($"[GameSmith] Server: {MCPServerConfig.DisplayName}");
                    UnityEngine.Debug.LogError($"[GameSmith] Command: {command} {string.Join(" ", args)}");
                    UnityEngine.Debug.LogError($"[GameSmith] IsConnected: {mcpClient.IsConnected}, Tools: {toolCount}");
                    UnityEngine.Debug.LogError($"[GameSmith] Make sure the MCP server is installed:");
                    UnityEngine.Debug.LogError($"  npm install -g {MCPServerConfig.ServerPackage}");
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
            var rulesButton = root.Q<Button>("rules-button");
            if (rulesButton != null)
            {
                rulesButton.clicked += () => GameSmithRulesWindow.ShowWindow();
            }

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
                viewLogsButton.text = "📊";
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
                // Diagnostic logging
                UnityEngine.Debug.Log($"[GameSmith] DirectMCP check: client={mcpClient != null}, connected={mcpClient?.IsConnected}, tools={mcpClient?.AvailableTools?.Count ?? 0}");

                // Check MCP availability
                if (mcpClient == null || !mcpClient.IsConnected || mcpClient.AvailableTools == null || mcpClient.AvailableTools.Count == 0)
                {
                    string reason = mcpClient == null ? "client not initialized" :
                                   !mcpClient.IsConnected ? "not connected" :
                                   mcpClient.AvailableTools == null || mcpClient.AvailableTools.Count == 0 ? "no tools" : "unknown";
                    AddMessageBubble($"❌ MCP not ready ({reason}). Restarting...", false);

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

            // Build system context using externalized prompts
            string systemContext = BuildSystemContext(tools);

            // Show processing indicator
            ShowProcessingIndicator();

            // Send to AI
            client.SendMessage(message, systemContext, tools,
                onSuccess: (response) => HandleAIResponse(response),
                onError: (error) => HandleError(error));
        }

        private void HandleAIResponse(AIResponse response)
        {
            // Remove processing indicator
            HideProcessingIndicator();

            // Track token usage
            if (response != null && response.TotalTokens > 0)
            {
                sessionInputTokens += response.InputTokens;
                sessionOutputTokens += response.OutputTokens;
                UpdateTokenDisplay();
            }

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

            // Handle tool use - support multiple tool uses in one response
            if (response.HasToolUse)
            {
                // Execute tool(s) via MCP
                if (mcpClient != null && (mcpClient.IsConnected || (mcpClient.AvailableTools != null && mcpClient.AvailableTools.Count > 0)))
                {
                    // Check if we have multiple tool uses
                    var toolUses = response.ToolUses != null && response.ToolUses.Count > 0
                        ? response.ToolUses
                        : new List<ToolUse> { new ToolUse { Id = response.ToolUseId, Name = response.ToolName, Input = response.ToolInput } };

                    if (toolUses.Count == 1)
                    {
                        // Single tool use - use existing logic
                        var toolUse = toolUses[0];
                        mcpClient.CallToolAsync(toolUse.Name, toolUse.Input, (toolResult) =>
                        {
                            // For Ollama, OpenAI, and Grok, display result and re-enable input
                            if (client.ActiveProvider.Contains("Ollama") || client.ActiveProvider.Contains("OpenAI") || client.ActiveProvider.Contains("Grok"))
                            {
                                // Parse and format the tool result for display
                                string formattedResult = FormatToolResult(toolUse.Name, toolResult);
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
                                var continuationContext = BuildSystemContext(continuationTools);

                                // Show processing indicator
                                ShowProcessingIndicator();

                                // Send tool result back to AI to continue conversation
                                client.SendToolResult(toolUse.Id, toolResult, continuationContext, continuationTools,
                                    onSuccess: (nextResponse) => HandleAIResponse(nextResponse),
                                    onError: (error) => HandleError(error));
                            }
                        });
                    }
                    else
                    {
                        // Multiple tool uses - execute all and send results together
                        ExecuteMultipleTools(toolUses);
                    }
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

        private void ExecuteMultipleTools(List<ToolUse> toolUses)
        {
            // Track tool execution results
            var toolResults = new Dictionary<string, string>();
            int completedCount = 0;
            int totalCount = toolUses.Count;

            // Execute each tool
            foreach (var toolUse in toolUses)
            {
                mcpClient.CallToolAsync(toolUse.Name, toolUse.Input, (toolResult) =>
                {
                    // Store result
                    toolResults[toolUse.Id] = toolResult;
                    completedCount++;

                    // Check if all tools completed
                    if (completedCount == totalCount)
                    {
                        // All tools executed, send results back to AI
                        SendMultipleToolResults(toolResults);
                    }
                });
            }
        }

        private void SendMultipleToolResults(Dictionary<string, string> toolResults)
        {
            // Build content blocks with all tool results
            var contentBlocks = new List<object>();

            foreach (var kvp in toolResults)
            {
                contentBlocks.Add(new Dictionary<string, object>
                {
                    { "type", "tool_result" },
                    { "tool_use_id", kvp.Key },
                    { "content", kvp.Value }
                });
            }

            // Get continuation context
            var continuationTools = mcpClient.AvailableTools;
            var continuationContext = BuildSystemContext(continuationTools);

            // Show processing indicator
            ShowProcessingIndicator();

            // Send all tool results back to AI to continue conversation
            client.SendMessage(null, continuationContext, contentBlocks, continuationTools,
                onSuccess: (nextResponse) => HandleAIResponse(nextResponse),
                onError: (error) => HandleError(error));
        }

        private void HandleError(string error)
        {
            // Remove processing indicator
            HideProcessingIndicator();

            AddErrorBubble($"Error: {error}");

            // Re-enable input
            messageInput.SetEnabled(true);
            if (sendButton != null)
            {
                sendButton.SetEnabled(true);
            }
            messageInput.Focus();
        }

        private void ShowProcessingIndicator()
        {
            if (messagesContainer == null) return;

            processingBubble = new VisualElement();
            processingBubble.AddToClassList("message-assistant");

            var label = new Label("⏳ Processing...");
            label.AddToClassList("message-assistant-text");
            label.style.opacity = 0.7f;

            processingBubble.Add(label);
            messagesContainer.Add(processingBubble);

            // Scroll to bottom
            chatScroll?.ScrollTo(processingBubble);
        }

        private void HideProcessingIndicator()
        {
            if (processingBubble != null && messagesContainer != null)
            {
                messagesContainer.Remove(processingBubble);
                processingBubble = null;
            }
        }

        private void UpdateTokenDisplay()
        {
            var mcpStatus = rootVisualElement.Q<Label>("mcp-status");
            if (mcpStatus != null)
            {
                int total = sessionInputTokens + sessionOutputTokens;
                if (total > 0)
                {
                    mcpStatus.text = $"⚒️ MCP: Ready | 🔢 Tokens: {total:N0} (↑{sessionInputTokens:N0} ↓{sessionOutputTokens:N0})";
                }
            }
        }

        private string BuildSystemContext(List<MCPTool> tools)
        {
            string basePrompt = (tools != null && tools.Count > 0)
                ? SystemPrompts.WithTools
                : SystemPrompts.WithoutTools;

            return basePrompt + "\n\n" + UnityProjectContext.GetProjectContext();
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

                // Reset token counters
                sessionInputTokens = 0;
                sessionOutputTokens = 0;

                // Update MCP status back to default
                var mcpStatus = rootVisualElement.Q<Label>("mcp-status");
                if (mcpStatus != null)
                {
                    mcpStatus.text = "⚒️ MCP: Ready";
                }

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

        #region MCP Testing and Diagnostics

        [MenuItem("Tools/GameSmith/Test MCP Connection", false, 100)]
        public static void TestMCPConnection()
        {
            var mcpClient = ConnectedMCPClient;

            if (mcpClient == null)
            {
                UnityEngine.Debug.LogError("[MCP Test] GameSmith window not open");
                EditorUtility.DisplayDialog("MCP Test", "Open GameSmith window first (Tools → GameSmith → Open Window)", "OK");
                return;
            }

            if (mcpClient.AvailableTools == null || mcpClient.AvailableTools.Count == 0)
            {
                UnityEngine.Debug.LogError("[MCP Test] No tools available");
                EditorUtility.DisplayDialog("MCP Test Failed", "No MCP tools loaded. Check Console for errors.", "OK");
                return;
            }

            // Get actual Unity scene object count
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            int actualCount = 0;
            foreach (var root in rootObjects)
            {
                actualCount += CountChildren(root.transform);
            }

            UnityEngine.Debug.Log($"[MCP Test] Unity scene has {actualCount} objects");
            UnityEngine.Debug.Log($"[MCP Test] Calling unity_get_hierarchy via MCP...");

            mcpClient.CallToolAsync("unity_get_hierarchy", new Dictionary<string, object>(),
                (result) =>
                {
                    UnityEngine.Debug.Log($"[MCP Test] Raw result: {result}");

                    // Parse MCP result - new format returns JSON with hierarchy array
                    int mcpCount = 0;
                    try
                    {
                        var resultObj = MiniJSON.Json.Deserialize(result) as Dictionary<string, object>;
                        if (resultObj != null && resultObj.ContainsKey("hierarchy"))
                        {
                            var hierarchy = resultObj["hierarchy"] as List<object>;
                            if (hierarchy != null)
                            {
                                mcpCount = hierarchy.Count;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[MCP Test] Failed to parse result: {ex.Message}");
                    }

                    UnityEngine.Debug.Log($"[MCP Test] MCP returned {mcpCount} objects");

                    if (mcpCount == actualCount)
                    {
                        UnityEngine.Debug.Log($"[MCP Test] ✓ SUCCESS - Counts match!");
                        EditorUtility.DisplayDialog("MCP Test Success",
                            $"✓ MCP Pipeline Working!\n\nUnity Scene: {actualCount} objects\nMCP Result: {mcpCount} objects\n\nAll systems operational.",
                            "OK");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[MCP Test] ⚠ Count mismatch - Unity: {actualCount}, MCP: {mcpCount}");
                        EditorUtility.DisplayDialog("MCP Test Warning",
                            $"MCP is connected but counts don't match:\n\nUnity Scene: {actualCount} objects\nMCP Result: {mcpCount} objects\n\nCheck Console for details.",
                            "OK");
                    }
                });
        }

        private static int CountChildren(Transform t)
        {
            int count = 1; // Count this object
            foreach (Transform child in t)
                count += CountChildren(child);
            return count;
        }

        [MenuItem("Tools/GameSmith/Debug MCP State", false, 101)]
        public static void DebugMCPState()
        {
            var mcpClient = ConnectedMCPClient;

            UnityEngine.Debug.Log("===== MCP State Debug =====");
            UnityEngine.Debug.Log($"MCP Client exists: {mcpClient != null}");

            if (mcpClient != null)
            {
                UnityEngine.Debug.Log($"IsConnected: {mcpClient.IsConnected}");
                UnityEngine.Debug.Log($"Tools Count: {mcpClient.AvailableTools?.Count ?? 0}");

                if (mcpClient.AvailableTools != null && mcpClient.AvailableTools.Count > 0)
                {
                    UnityEngine.Debug.Log("\nFirst 10 available tools:");
                    foreach (var tool in mcpClient.AvailableTools.Take(10))
                    {
                        UnityEngine.Debug.Log($"  • {tool.Name}: {tool.Description}");
                    }

                    EditorUtility.DisplayDialog("MCP Debug",
                        $"MCP Client: Active\nConnected: {mcpClient.IsConnected}\nTools: {mcpClient.AvailableTools.Count}\n\nCheck Console for tool list.",
                        "OK");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No tools loaded!");
                    EditorUtility.DisplayDialog("MCP Debug",
                        $"MCP Client: Active\nConnected: {mcpClient.IsConnected}\nTools: 0\n\nNo tools available!",
                        "OK");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("MCP client is null!");
                EditorUtility.DisplayDialog("MCP Debug", "MCP client not initialized.\n\nPlease open GameSmith window first.", "OK");
            }
        }

        #endregion
    }
}
