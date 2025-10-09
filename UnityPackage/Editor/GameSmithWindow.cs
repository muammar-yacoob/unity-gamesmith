using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
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
        private Button sendButton;
        private PopupField<string> modelDropdown;
        private Label providerStatus;

        // Coroutine support
        private IEnumerator currentCoroutine;

        // Ollama status
        private bool isOllamaRunning = false;
        private float lastOllamaCheck = 0f;

        [MenuItem("Tools/Game Smith &G", false, 1)]
        public static void ShowWindow()
        {
            var hierarchyWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var window = GetWindow<GameSmithWindow>("Game Smith", false, hierarchyWindowType);
            window.minSize = new Vector2(400, 500);
            window.Show();
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
            EditorApplication.update += CheckOllamaStatus;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= UpdateCoroutine;
            EditorApplication.update -= CheckConfigChanges;
            EditorApplication.update -= CheckOllamaStatus;
        }

        private string lastConfigState = "";

        private void CheckConfigChanges()
        {
            if (config == null) return;

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
            sendButton = root.Q<Button>("send-button");
            providerStatus = root.Q<Label>("provider-status");

            // Create model dropdown programmatically
            var modelDropdownContainer = root.Q<VisualElement>("model-dropdown-container");
            if (modelDropdownContainer != null)
            {
                var models = config.GetModelsList();
                var currentModel = config.GetCurrentModel();

                modelDropdown = new PopupField<string>(models, currentModel);
                modelDropdown.RegisterValueChangedCallback(evt => OnModelChanged(evt.newValue));
                modelDropdownContainer.Add(modelDropdown);
            }

            UpdateProviderStatus();

            // Make provider status clickable
            if (providerStatus != null)
            {
                providerStatus.RegisterCallback<ClickEvent>(OnProviderStatusClicked);
                providerStatus.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor() { texture = null });
            }

            // Setup callbacks
            var settingsButton = root.Q<Button>("settings-button");
            settingsButton.clicked += OpenConfigInInspector;

            var clearButton = root.Q<Button>("clear-button");
            clearButton.clicked += ClearChat;

            sendButton.clicked += SendMessage;

            // Enter to send
            messageInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
                {
                    evt.PreventDefault();
                    SendMessage();
                }
            });
        }

        private void OpenConfigInInspector()
        {
            // Select and highlight the config ScriptableObject in Inspector
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        private void OnModelChanged(string newModel)
        {
            config.selectedModel = newModel;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            UpdateProviderStatus();
        }

        private void UpdateProviderStatus()
        {
            if (providerStatus == null || config == null) return;

            if (!config.IsValid())
            {
                providerStatus.text = "⚠ Not configured";
                providerStatus.style.color = new StyleColor(new Color(0.9f, 0.6f, 0.4f));
                return;
            }

            // Check if this is Ollama
            bool isOllama = config.apiUrl.Contains("localhost:11434") ||
                           config.apiUrl.Contains("127.0.0.1:11434") ||
                           config.providerName.ToLower().Contains("ollama");

            if (isOllama)
            {
                if (isOllamaRunning)
                {
                    providerStatus.text = $"✓ {config.providerName} (Running)";
                    providerStatus.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
                }
                else
                {
                    providerStatus.text = $"⚠ {config.providerName} (Stopped) - Click to start";
                    providerStatus.style.color = new StyleColor(new Color(0.9f, 0.6f, 0.4f));
                }
            }
            else
            {
                providerStatus.text = $"✓ {config.providerName}";
                providerStatus.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
            }
        }

        private void OnProviderStatusClicked(ClickEvent evt)
        {
            bool isOllama = config.apiUrl.Contains("localhost:11434") ||
                           config.apiUrl.Contains("127.0.0.1:11434") ||
                           config.providerName.ToLower().Contains("ollama");

            if (isOllama && !isOllamaRunning)
            {
                StartOllama();
            }
        }

        private void CheckOllamaStatus()
        {
            if (config == null) return;

            // Check if this is Ollama
            bool isOllama = config.apiUrl.Contains("localhost:11434") ||
                           config.apiUrl.Contains("127.0.0.1:11434") ||
                           config.providerName.ToLower().Contains("ollama");

            if (!isOllama) return;

            // Check every 5 seconds
            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - lastOllamaCheck < 5f) return;

            lastOllamaCheck = currentTime;

            // Check if Ollama is running
            StartCoroutine(CheckOllamaRunning());
        }

        private IEnumerator CheckOllamaRunning()
        {
            using (var request = UnityWebRequest.Get("http://localhost:11434/api/tags"))
            {
                request.timeout = 2;
                yield return request.SendWebRequest();

                bool wasRunning = isOllamaRunning;
                isOllamaRunning = request.result == UnityWebRequest.Result.Success;

                if (wasRunning != isOllamaRunning)
                {
                    UpdateProviderStatus();
                }
            }
        }

        private void StartOllama()
        {
            try
            {
                // Try to start Ollama
                var startInfo = new ProcessStartInfo();

                #if UNITY_EDITOR_WIN
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/c start ollama serve";
                startInfo.UseShellExecute = true;
                startInfo.CreateNoWindow = true;
                #elif UNITY_EDITOR_OSX
                startInfo.FileName = "open";
                startInfo.Arguments = "-a Ollama";
                #else
                startInfo.FileName = "ollama";
                startInfo.Arguments = "serve";
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                #endif

                Process.Start(startInfo);

                UnityEngine.Debug.Log("Starting Ollama...");

                // Recheck status after a delay
                EditorApplication.delayCall += () =>
                {
                    System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                    {
                        EditorApplication.delayCall += () => StartCoroutine(CheckOllamaRunning());
                    });
                };
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Could not start Ollama.\n\nPlease start it manually or install from: https://ollama.ai\n\nError: {e.Message}",
                    "OK");
            }
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

            // Clear input
            messageInput.value = "";
            sendButton.SetEnabled(false);

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
            sendButton.SetEnabled(true);
        }

        private void OnAIError(string error)
        {
            AddErrorBubble($"Error: {error}");
            sendButton.SetEnabled(true);
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

            var label = new Label(text);
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
