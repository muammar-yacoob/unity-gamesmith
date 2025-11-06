using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SparkGames.UnityGameSmith.Editor
{
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

        // UI Elements
        private TextField messageInput;
        private VisualElement messagesContainer;
        private ScrollView chatScroll;
        private PopupField<string> modelDropdown;
        private Label providerStatus;
        private Button sendButton;

        [MenuItem("Tools/GameSmith/GameSmith AI &g", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<GameSmithWindow>("GameSmith AI");
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
            // Load config and history
            config = GameSmithConfig.GetOrCreate();
            history = ChatHistory.GetOrCreate();
            client = new AIAgentClient(config);

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
        }

        private void StartMCPServerAsync()
        {
            // Check if MCP server is already running
            if (mcpClient != null && mcpClient.IsConnected)
            {
                AddMessageBubble("✓ MCP server is already running", false);
                return;
            }

            AddMessageBubble("Starting MCP server...", false);

            if (mcpClient == null)
            {
                mcpClient = new MCPClientAsync();
            }

            // Windows only - use the direct path to the globally installed package
            string appData = System.Environment.GetEnvironmentVariable("APPDATA");
            string packagePath = System.IO.Path.Combine(appData, "npm", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js");

            if (!System.IO.File.Exists(packagePath))
            {
                UnityEngine.Debug.LogError($"[GameSmith] unity-mcp not found at {packagePath}");
                UnityEngine.Debug.LogError("[GameSmith] Please install: npm install -g @spark-apps/unity-mcp");
                AddMessageBubble("❌ MCP not installed. Run: npm install -g @spark-apps/unity-mcp", false);
                return;
            }

            string nodePath = "node";
            string[] args = new string[] { packagePath };

            // Starting MCP server

            // Start server without blocking
            mcpClient.StartServerAsync(nodePath, args, (success) =>
            {
                var mcpStatus = rootVisualElement?.Q<Label>("mcp-status");
                if (success)
                {
                    UnityEngine.Debug.Log($"[MCP] Ready: {mcpClient.AvailableTools.Count} tools");
                    AddMessageBubble($"MCP ready ({mcpClient.AvailableTools.Count} tools)", false);
                    if (mcpStatus != null)
                    {
                        mcpStatus.text = $"✓ MCP: {mcpClient.AvailableTools.Count} tools";
                        mcpStatus.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[GameSmith] Failed to start MCP server");
                    AddMessageBubble("❌ MCP failed to start. Check console for details.", false);
                    if (mcpStatus != null)
                    {
                        mcpStatus.text = "❌ MCP: Failed";
                        mcpStatus.style.color = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
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

            // Add MCP control button
            var controlsContainer = root.Q<VisualElement>("controls-container");
            if (controlsContainer == null)
            {
                // Create controls container if it doesn't exist
                controlsContainer = new VisualElement();
                controlsContainer.style.flexDirection = FlexDirection.Row;
                controlsContainer.style.marginTop = 5;
                controlsContainer.style.marginBottom = 5;
                root.Insert(0, controlsContainer);
            }

            // MCP status label (read-only, not a button)
            var mcpStatus = new Label("⏳ MCP: Starting...");
            mcpStatus.name = "mcp-status";
            mcpStatus.style.fontSize = 11;
            mcpStatus.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            mcpStatus.style.marginLeft = 10;
            mcpStatus.style.unityTextAlign = TextAnchor.MiddleLeft;
            controlsContainer.Add(mcpStatus);

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
                    // Display human-friendly names from config while keeping ids as values
                    modelDropdown.formatSelectedValueCallback = (val) => config.GetModelDisplayName(val);
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

            // Add View Logs button
            var viewLogsButton = new Button(() => GameSmithLogger.OpenLogFolder());
            viewLogsButton.text = "📝 View Logs";
            viewLogsButton.tooltip = "Open log folder to view detailed logs";
            viewLogsButton.style.marginLeft = 5;

            var buttonsContainer = root.Q<VisualElement>("buttons-container");
            if (buttonsContainer != null)
            {
                buttonsContainer.Add(viewLogsButton);
            }
            else if (clearButton != null)
            {
                // Add after clear button if container not found
                clearButton.parent.Add(viewLogsButton);
            }

            if (sendButton != null)
            {
                sendButton.clicked += SendMessage;
            }

            // Enter to send (Return and KeypadEnter), Shift+Enter inserts newline
            if (messageInput != null)
            {
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

        private void SendMessage()
        {
            var message = messageInput.value;
            if (string.IsNullOrWhiteSpace(message)) return;

            // Add user message
            AddMessageBubble(message, true);
            history.AddMessage(ChatMessage.Role.User, message);

            // Clear input and disable during processing
            messageInput.value = "";
            messageInput.SetEnabled(false);
            if (sendButton != null)
            {
                sendButton.SetEnabled(false);
            }

            // Get Unity project context
            var systemContext = @"You are a Unity AI assistant. Be concise and direct.

IMPORTANT: You have access to Unity scene manipulation tools via MCP. When the user asks to modify Unity objects (create, move, scale, rotate, delete), you MUST use the available tools.

Examples:
- ""make selected cube 3x taller"" → use scale_object tool with scale {x:1, y:3, z:1}
- ""create a sphere"" → use create_object tool with type 'Sphere'
- ""move player forward"" → use translate_object tool
- ""rotate camera"" → use rotate_object tool

Always use tools for Unity scene modifications. Do not just explain - actually execute the tool.

" + UnityProjectContext.GetProjectContext();

            // Get MCP tools if available
            var tools = mcpClient != null && mcpClient.IsConnected ? mcpClient.AvailableTools : null;

            // Send to AI with tools
            client.SendMessage(message, systemContext, tools,
                onSuccess: (response) => HandleAIResponse(response),
                onError: (error) => HandleError(error));
        }

        private void HandleAIResponse(AIResponse response)
        {
            // Display text content if any
            if (!string.IsNullOrEmpty(response.TextContent))
            {
                AddMessageBubble(response.TextContent, false);
                history.AddMessage(ChatMessage.Role.Assistant, response.TextContent);
            }

            // Handle tool use
            if (response.HasToolUse)
            {
                AddMessageBubble($"[Using tool: {response.ToolName}]", false);

                // Execute tool asynchronously
                if (mcpClient != null && mcpClient.IsConnected)
                {
                    mcpClient.CallToolAsync(response.ToolName, response.ToolInput, (toolResult) =>
                    {
                        // Add tool result to chat
                        AddMessageBubble($"[Tool result: {toolResult}]", false);

                        // Get tools and system context again
                        var tools = mcpClient.AvailableTools;
                        var systemContext = @"You are a Unity AI assistant. Be concise and direct.

IMPORTANT: You have access to Unity scene manipulation tools via MCP. When the user asks to modify Unity objects (create, move, scale, rotate, delete), you MUST use the available tools.

Examples:
- ""make selected cube 3x taller"" → use scale_object tool with scale {x:1, y:3, z:1}
- ""create a sphere"" → use create_object tool with type 'Sphere'
- ""move player forward"" → use translate_object tool
- ""rotate camera"" → use rotate_object tool

Always use tools for Unity scene modifications. Do not just explain - actually execute the tool.

" + UnityProjectContext.GetProjectContext();

                        // Send tool result back to AI to continue conversation
                        client.SendToolResult(response.ToolUseId, toolResult, systemContext, tools,
                            onSuccess: (nextResponse) => HandleAIResponse(nextResponse),
                            onError: (error) => HandleError(error));
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
            var bubble = new VisualElement();
            bubble.AddToClassList(isUser ? "message-user" : "message-assistant");

            var label = new Label(text);
            label.AddToClassList(isUser ? "message-user-text" : "message-assistant-text");

            bubble.Add(label);
            messagesContainer.Add(bubble);

            // Scroll to bottom
            EditorApplication.delayCall += () =>
            {
                chatScroll.scrollOffset = new Vector2(0, chatScroll.contentContainer.layout.height);
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
