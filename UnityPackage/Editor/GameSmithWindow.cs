using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;

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

            // Add MCP status message
            AddMessageBubble("💬 Chat ready. Click 'Start MCP' button below to enable Unity scene manipulation features.", false);

            // Enable config updates
            EditorApplication.update += CheckConfigChanges;
        }

        private void StartMCPServerAsync()
        {
            // Check if MCP server is already running
            if (mcpClient != null && mcpClient.IsConnected)
            {
                UnityEngine.Debug.Log("[GameSmith] MCP server is already running.");
                AddMessageBubble("✓ MCP server is already running", false);
                return;
            }

            AddMessageBubble("🔄 Starting MCP server...", false);

            // Find node path asynchronously
            EditorCoroutineRunner.StartCoroutine(FindNodeAndStartMCP());
        }

        private IEnumerator FindNodeAndStartMCP()
        {
            string nodePath = null;

            // On Windows, try to find node.exe
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Try common locations first
                string[] commonPaths = new string[]
                {
                    @"C:\Program Files\nodejs\node.exe",
                    @"C:\Program Files (x86)\nodejs\node.exe",
                    System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("ProgramFiles"), "nodejs", "node.exe"),
                    System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Programs", "nodejs", "node.exe")
                };

                foreach (var path in commonPaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        nodePath = path;
                        break;
                    }
                }

                // If not found, try using 'where' command (but non-blocking)
                if (string.IsNullOrEmpty(nodePath))
                {
                    UnityEngine.Debug.LogWarning("[GameSmith] Node.js not found in common locations. Please ensure Node.js is installed.");
                    AddMessageBubble("❌ Node.js not found. Please install Node.js and try again.", false);
                    yield break;
                }
            }
            else
            {
                // On Mac/Linux, use npx directly
                nodePath = "npx";
            }

            // Try to find or use unity-mcp
            if (mcpClient == null)
            {
                mcpClient = new MCPClientAsync();
            }

            string[] args;
            if (Application.platform == RuntimePlatform.WindowsEditor && !string.IsNullOrEmpty(nodePath))
            {
                // Try to use globally installed package
                string npmPrefix = System.Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(npmPrefix))
                {
                    npmPrefix = System.IO.Path.GetDirectoryName(npmPrefix); // Get parent of AppData\Roaming
                    string globalPath = System.IO.Path.Combine(npmPrefix, "AppData", "Roaming", "npm", "node_modules", "@spark-apps", "unity-mcp", "dist", "index.js");
                    if (System.IO.File.Exists(globalPath))
                    {
                        args = new string[] { globalPath };
                    }
                    else
                    {
                        // Fallback to npx
                        args = new string[] { "-c", "npx -y @spark-apps/unity-mcp" };
                        nodePath = "cmd.exe";
                    }
                }
                else
                {
                    args = new string[] { "-c", "npx -y @spark-apps/unity-mcp" };
                    nodePath = "cmd.exe";
                }
            }
            else
            {
                args = new string[] { "-y", "@spark-apps/unity-mcp" };
            }

            // Start server asynchronously
            mcpClient.StartServerAsync(nodePath, args, (success) =>
            {
                if (success)
                {
                    UnityEngine.Debug.Log($"[GameSmith] MCP server connected with {mcpClient.AvailableTools.Count} tools");
                    AddMessageBubble($"✓ MCP server started ({mcpClient.AvailableTools.Count} tools available)", false);

                    // Update button
                    var mcpButton = rootVisualElement.Q<Button>("mcp-button");
                    if (mcpButton != null)
                    {
                        mcpButton.text = "MCP Running";
                        mcpButton.SetEnabled(false);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[GameSmith] Failed to start MCP server");
                    AddMessageBubble("❌ Failed to start MCP server. Check console for details.", false);
                }
            });

            yield return null;
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

            var mcpButton = new Button(() => StartMCPServerAsync());
            mcpButton.text = "Start MCP Server";
            mcpButton.name = "mcp-button";
            controlsContainer.Add(mcpButton);

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
            var systemContext = @"You are a Unity development AI assistant. You have access to the user's Unity project context.

Help with:
- Writing C# scripts
- Explaining Unity concepts
- Debugging issues
- Suggesting solutions
- Analyzing project structure

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
                        var systemContext = @"You are a Unity development AI assistant. You have access to the user's Unity project context and Unity MCP tools.

Help with:
- Writing C# scripts
- Explaining Unity concepts
- Debugging issues
- Suggesting solutions
- Analyzing project structure

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
