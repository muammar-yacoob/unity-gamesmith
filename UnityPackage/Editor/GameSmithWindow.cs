using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple AI chat window for Unity development
    /// </summary>
    public class GameSmithWindow : EditorWindow
    {
        private GameSmithConfig config;
        private ChatHistory history;
        private AIAgentClient client;

        // UI Elements
        private TextField messageInput;
        private VisualElement messagesContainer;
        private ScrollView chatScroll;
        private PopupField<string> modelDropdown;
        private Label providerStatus;
        private Button sendButton;

        // Coroutine support
        private IEnumerator currentCoroutine;

        [MenuItem("Tools/GameSmith/Open Window &g", false, 1)]
        public static void ShowWindow()
        {
            var hierarchyWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var window = GetWindow<GameSmithWindow>("GameSmith", false, hierarchyWindowType);
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        [MenuItem("Tools/GameSmith/Configure Settings", false, 2)]
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

            // Enable coroutine and config updates
            EditorApplication.update += UpdateCoroutine;
            EditorApplication.update += CheckConfigChanges;
        }

        private void OnDestroy()
        {
            // Unregister all callbacks
            EditorApplication.update -= UpdateCoroutine;
            EditorApplication.update -= CheckConfigChanges;

            // Clear coroutine
            currentCoroutine = null;

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
            var currentState = $"{config.apiUrl}|{config.apiKey}|{config.availableModels}|{config.selectedModel}";

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

        private void StartCoroutine(IEnumerator routine)
        {
            currentCoroutine = routine;
        }

        private void UpdateCoroutine()
        {
            if (currentCoroutine != null)
            {
                if (!currentCoroutine.MoveNext())
                {
                    currentCoroutine = null;
                }
            }
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

            // Populate Ollama models if active and empty
            StartCoroutine(LoadOllamaModelsIfNeeded());

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
                settingsButton.clicked += OpenConfigInInspector;
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

            // Send to AI
            StartCoroutine(client.SendMessageAsync(message, systemContext, OnAIResponse, OnAIError));
        }

        private void OnAIResponse(string response)
        {
            AddMessageBubble(response, false);
            history.AddMessage(ChatMessage.Role.Assistant, response);
            messageInput.SetEnabled(true);
            if (sendButton != null)
            {
                sendButton.SetEnabled(true);
            }
            messageInput.Focus();
        }

        private void OnAIError(string error)
        {
            AddErrorBubble($"Error: {error}");
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
                OpenConfigInInspector();
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
