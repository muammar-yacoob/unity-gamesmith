using System;
using System.Collections.Generic;
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
        private MCPClient mcpClient;

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

            // Load UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.spark-games.unity-gamesmith/Editor/GameSmithWindow.uxml");

            if (visualTree == null)
            {
                var label = new Label("GameSmithWindow.uxml not found!");
                label.style.paddingTop = 20;
                label.style.paddingLeft = 20;
                rootVisualElement.Add(label);
                return;
            }

            var root = visualTree.CloneTree();
            rootVisualElement.Add(root);

            // Load USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.spark-games.unity-gamesmith/Editor/GameSmithWindow.uss");

            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            InitializeUI(root);
            LoadChatHistory();

            // Start MCP server after UI is ready
            EditorApplication.delayCall += () => StartMCPServer();

            // Enable config updates
            EditorApplication.update += CheckConfigChanges;
        }

        private void StartMCPServer()
        {
            // Check if MCP server is already running
            if (mcpClient != null && mcpClient.IsConnected)
            {
                UnityEngine.Debug.Log("[GameSmith] MCP server is already running.");
                return;
            }

            try
            {
                string executablePath = "";
                string[] argsArray;

                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    // On Windows, find node.exe
                    var whereNode = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c where node",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    whereNode.Start();
                    string nodePath = whereNode.StandardOutput.ReadLine()?.Trim();
                    whereNode.WaitForExit();

                    if (string.IsNullOrEmpty(nodePath))
                    {
                        throw new Exception("node.exe not found. Please ensure Node.js is installed and in PATH.");
                    }

                    // Find the npm prefix directory
                    var nodeDir = System.IO.Path.GetDirectoryName(nodePath);

                    // npx-cli.js is typically at: <node_dir>/node_modules/npm/bin/npx-cli.js
                    string npxCliPath = System.IO.Path.Combine(nodeDir, "node_modules", "npm", "bin", "npx-cli.js");

                    if (!System.IO.File.Exists(npxCliPath))
                    {
                        // Try alternate location: node_modules/npm/bin/npx-cli.js relative to node.exe
                        npxCliPath = System.IO.Path.Combine(nodeDir, "..", "node_modules", "npm", "bin", "npx-cli.js");
                        npxCliPath = System.IO.Path.GetFullPath(npxCliPath);
                    }

                    if (!System.IO.File.Exists(npxCliPath))
                    {
                        throw new Exception($"npx-cli.js not found. Searched at: {npxCliPath}");
                    }

                    UnityEngine.Debug.Log($"[GameSmith] Using node.exe at: {nodePath}");
                    UnityEngine.Debug.Log($"[GameSmith] Using npx-cli.js at: {npxCliPath}");

                    // Run: node.exe <npx-cli.js> -y @spark-apps/unity-mcp
                    executablePath = nodePath;
                    argsArray = new string[] { npxCliPath, "-y", "@spark-apps/unity-mcp" };
                }
                else // macOS or Linux
                {
                    executablePath = "npx";
                    argsArray = new string[] { "-y", "@spark-apps/unity-mcp" };
                }

                // Create and start MCP client
                if (mcpClient == null)
                {
                    mcpClient = new MCPClient();
                }

                bool mcpStarted = mcpClient.StartServer(executablePath, argsArray);

                if (mcpStarted && mcpClient.IsConnected)
                {
                    UnityEngine.Debug.Log($"[GameSmith] MCP server connected with {mcpClient.AvailableTools.Count} tools available.");
                    AddMessageBubble($"✓ Unity MCP server started ({mcpClient.AvailableTools.Count} tools available)", false);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[GameSmith] MCP server failed to start or connect.");
                    AddMessageBubble("⚠ Unity MCP failed to start. Chat works but Unity scene manipulation is disabled.", false);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Exception while starting MCP server: {ex.Message}");
                AddMessageBubble($"⚠ Unity MCP not available: {ex.Message}\nChat works but Unity scene manipulation is disabled.", false);
            }
        }

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

                // Execute tool
                if (mcpClient != null && mcpClient.IsConnected)
                {
                    var toolResult = mcpClient.CallTool(response.ToolName, response.ToolInput);

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
