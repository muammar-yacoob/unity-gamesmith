using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;

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

                // Validation status
                if (!string.IsNullOrEmpty(currentKey))
                {
                    if (isVerified)
                    {
                        EditorGUILayout.HelpBox("✅ API key verified and working", MessageType.Info);
                    }
                    else if (hasValidFormat)
                    {
                        EditorGUILayout.HelpBox("⚠️ API key format is valid but not verified yet", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("❌ API key format appears invalid for " + config.activeProvider, MessageType.Error);
                    }
                }

                // Show verification message if any
                if (!string.IsNullOrEmpty(verificationMessage))
                {
                    EditorGUILayout.HelpBox(verificationMessage, isVerified ? MessageType.Info : MessageType.Error);
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
    }
}
