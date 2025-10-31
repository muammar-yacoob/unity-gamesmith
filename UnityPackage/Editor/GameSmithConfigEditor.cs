using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Custom editor for GameSmithConfig with collapsible groups
    /// </summary>
    [CustomEditor(typeof(GameSmithConfig))]
    public class GameSmithConfigEditor : UnityEditor.Editor
    {
        private GameSmithConfig config;

        // Coroutine management
        private Dictionary<ProviderConfiguration, EditorCoroutine> activeVerifications = new Dictionary<ProviderConfiguration, EditorCoroutine>();

        // Style caches
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;

        // UI state
        private bool modelParametersFoldout = true;
        private Dictionary<string, float> lastApiKeyChangeTime = new Dictionary<string, float>();

        private void OnEnable()
        {
            config = (GameSmithConfig)target;
            // Force a reload of providers from JSON every time the editor is enabled.
            // This ensures it's always up-to-date with the user's AIModels.json file.
            config.ForceLoadProviders();
            Repaint();
        }

        private void OnDisable()
        {
            // Stop all active verifications
            foreach (var kvp in activeVerifications)
            {
                if (kvp.Value != null)
                    kvp.Value.Stop();
            }

            activeVerifications.Clear();
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(12, 12, 10, 10),
                    margin = new RectOffset(0, 0, 0, 0)
                };
            }
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(12);

            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("GameSmith Configuration", titleStyle);

            EditorGUILayout.Space(15);

            // Main Settings
            DrawProviderSelector();

            EditorGUILayout.Space(12);

            DrawModelParameters();

            EditorGUILayout.Space(15);

            // Footer actions
            DrawFooterActions();

            EditorGUILayout.Space(10);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProviderSelector()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // Provider Selection with Status
            EditorGUILayout.BeginHorizontal();

            string[] providerNames = config.Providers.Select(p => p.providerName).ToArray();
            int currentProviderIndex = System.Array.IndexOf(providerNames, config.activeProvider);
            if (currentProviderIndex < 0)
            {
                currentProviderIndex = 0;
                if (providerNames.Length > 0)
                {
                    config.activeProvider = providerNames[0];
                }
            }

            EditorGUILayout.PrefixLabel("Active Provider");
            int newProviderIndex = EditorGUILayout.Popup(currentProviderIndex, providerNames, GUILayout.MinWidth(150));

            if (newProviderIndex != currentProviderIndex && newProviderIndex >= 0)
            {
                config.activeProvider = providerNames[newProviderIndex];
                var provider = config.GetActiveProvider();
                if (provider != null && provider.models.Count > 0)
                {
                    config.selectedModel = provider.models[0].modelName;
                }
                // Reset verification status when changing providers
                if (provider != null)
                {
                    provider.verificationStatus = VerificationStatus.NotConfigured;

                    // Auto-verify Ollama when selected to fetch models
                    if (provider.providerName.ToLower().Contains("ollama"))
                    {
                        StartVerification(provider);
                    }
                    // Auto-verify other providers if they have an API key
                    else if (!string.IsNullOrEmpty(provider.apiKey))
                    {
                        StartVerification(provider);
                    }
                }
            }

            // Status indicator
            var activeProvider = config.GetActiveProvider();
            DrawStatusIndicator(activeProvider);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);


            // Selected Model
            if (activeProvider != null && activeProvider.models != null && activeProvider.models.Count > 0)
            {
                string[] modelNames = activeProvider.models
                    .Where(m => m.isEnabled)
                    .Select(m => string.IsNullOrEmpty(m.displayName) ? m.modelName : m.displayName)
                    .ToArray();
                string[] modelValues = activeProvider.models
                    .Where(m => m.isEnabled)
                    .Select(m => m.modelName)
                    .ToArray();

                int currentModelIndex = System.Array.IndexOf(modelValues, config.selectedModel);
                if (currentModelIndex < 0 && modelValues.Length > 0)
                {
                    currentModelIndex = 0;
                    config.selectedModel = modelValues[0];
                }

                int newModelIndex = EditorGUILayout.Popup("Selected Model", currentModelIndex, modelNames);
                if (newModelIndex != currentModelIndex && newModelIndex >= 0 && newModelIndex < modelValues.Length)
                {
                    config.selectedModel = modelValues[newModelIndex];
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No models available for this provider.", MessageType.Warning);
            }

            EditorGUILayout.Space(12);

            // Provider Settings
            bool isOllama = activeProvider != null && activeProvider.providerName.ToLower().Contains("ollama");

            if (isOllama)
            {
                DrawOllamaSettings(activeProvider);
            }
            else if (activeProvider != null)
            {
                DrawProviderSettings(activeProvider);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProviderSettings(ProviderConfiguration provider)
        {
            EditorGUILayout.LabelField("Provider Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // API Key
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PrefixLabel("API Key");
            string newKey = EditorGUILayout.PasswordField(provider.apiKey);

            if (EditorGUI.EndChangeCheck())
            {
                provider.apiKey = newKey;

                // Cancel any existing verification
                if (activeVerifications.ContainsKey(provider))
                {
                    activeVerifications[provider]?.Stop();
                    activeVerifications.Remove(provider);
                }

                // Auto-verify after a short delay when API key changes
                if (!string.IsNullOrEmpty(newKey))
                {
                    string providerKey = $"{provider.providerName}_api_key";
                    lastApiKeyChangeTime[providerKey] = Time.realtimeSinceStartup;

                    // Start verification after 1 second delay
                    var coroutine = new EditorCoroutine(DelayedVerification(provider, 1.0f));
                    coroutine.Start();
                }
                else
                {
                    provider.verificationStatus = VerificationStatus.NotConfigured;
                    provider.verificationMessage = "";
                }
            }

            EditorGUILayout.EndHorizontal();

            // Get API Key link
            if (!string.IsNullOrEmpty(provider.apiKeyUrl))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Get API Key ↗", EditorStyles.linkLabel))
                {
                    Application.OpenURL(provider.apiKeyUrl);
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Only show error messages, not success messages
            if (provider.verificationStatus == VerificationStatus.Failed && !string.IsNullOrEmpty(provider.verificationMessage))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(provider.verificationMessage, MessageType.Error);
            }
        }

        private void DrawOllamaSettings(ProviderConfiguration provider)
        {
            EditorGUILayout.LabelField("Ollama Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Server Status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Server Status");
            string statusText = GetOllamaStatusText(provider.verificationStatus);
            var statusColor = GetStatusLabelColor(provider.verificationStatus);

            var oldColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            GUILayout.Label(statusText, EditorStyles.label);
            GUI.contentColor = oldColor;
            EditorGUILayout.EndHorizontal();

            // Start Ollama button if not connected
            if (provider.verificationStatus == VerificationStatus.Failed ||
                provider.verificationStatus == VerificationStatus.NotConfigured)
            {
                EditorGUILayout.Space(8);
                if (GUILayout.Button("Start Ollama Server", GUILayout.Height(28)))
                {
                    StartOllamaServer(provider);
                }
            }

            // Only show error messages
            if (provider.verificationStatus == VerificationStatus.Failed && !string.IsNullOrEmpty(provider.verificationMessage))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(provider.verificationMessage, MessageType.Warning);
            }
        }

        private void DrawModelParameters()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // Foldout header with reset defaults hyperlink
            EditorGUILayout.BeginHorizontal();
            modelParametersFoldout = EditorGUILayout.Foldout(modelParametersFoldout, "Model Parameters", true, EditorStyles.boldLabel);

            if (modelParametersFoldout)
            {
                GUILayout.FlexibleSpace();

                // Reset defaults hyperlink
                var linkStyle = new GUIStyle(EditorStyles.linkLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.5f, 0.7f, 1f) }
                };

                if (GUILayout.Button("Reset defaults", linkStyle))
                {
                    ResetModelParametersToDefaults();
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            EditorGUILayout.EndHorizontal();

            if (modelParametersFoldout)
            {
                EditorGUILayout.Space(8);

                config.temperature = EditorGUILayout.Slider("Temperature", config.temperature, 0f, 2f);
                EditorGUILayout.Space(4);

                config.maxTokens = EditorGUILayout.IntSlider("Max Tokens", config.maxTokens, 100, 8192);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooterActions()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Configure hyperlink style
            var linkStyle = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = 12
            };

            if (GUILayout.Button("Configure", linkStyle))
            {
                SelectAIModelsAsset();
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProviderConfigurations()
        {
            var activeProvider = config.GetActiveProvider();
            if (activeProvider == null) return;

            EditorGUILayout.LabelField("API Configuration", headerStyle);
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(boxStyle);

            UpdateVerificationStatus(activeProvider);

            bool isOllama = activeProvider.providerName.ToLower().Contains("ollama");

            if (!isOllama)
            {
                DrawAPIKeyField(activeProvider);
            }
            else
            {
                DrawOllamaInfo(activeProvider);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAPIKeyField(ProviderConfiguration provider)
        {
            // API Key with inline status
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("APIKeyField");

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUILayout.PrefixLabel("API Key");

            string newKey = EditorGUILayout.PasswordField(provider.apiKey);

            // Status indicator
            Color statusColor = GetStatusColor(provider.verificationStatus);
            GUI.color = statusColor;
            GUILayout.Label(GetStatusIcon(provider.verificationStatus), GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            bool keyChanged = EditorGUI.EndChangeCheck();
            if (keyChanged && newKey != provider.apiKey)
            {
                provider.apiKey = newKey;
                provider.verificationStatus = VerificationStatus.NotConfigured;
            }

            // Get API Key link
            if (!string.IsNullOrEmpty(provider.apiKeyUrl))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                var linkStyle = new GUIStyle(EditorStyles.linkLabel)
                {
                    fontSize = 11
                };

                if (GUILayout.Button("Get API Key ↗", linkStyle))
                {
                    Application.OpenURL(provider.apiKeyUrl);
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(3);
            }

            // Auto-verify on Enter
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "APIKeyField")
            {
                if (!string.IsNullOrEmpty(provider.apiKey))
                {
                    VerifyProviderAPI(provider);
                    GUI.FocusControl(null);
                }
                Event.current.Use();
                Repaint();
            }

            // Status messages
            DrawStatusMessages(provider);
        }

        private void DrawOllamaInfo(ProviderConfiguration provider)
        {
            EditorGUILayout.HelpBox("Ollama runs locally. No API key required.", MessageType.None);

            if (provider.verificationStatus == VerificationStatus.NotConfigured)
            {
                // Throttle verification attempts from OnGUI
                if (Time.realtimeSinceStartup - provider.lastVerificationTime > 5f)
                {
                    VerifyProviderAPI(provider);
                }
            }
            else if (provider.verificationStatus == VerificationStatus.Failed)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(provider.verificationMessage, MessageType.Error);
            }
        }

        private void DrawStatusMessages(ProviderConfiguration provider)
        {
            if (provider.verificationStatus == VerificationStatus.Failed &&
                !string.IsNullOrEmpty(provider.verificationMessage))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(provider.verificationMessage, MessageType.Error);
            }
            else if (provider.verificationStatus == VerificationStatus.NotConfigured &&
                     string.IsNullOrEmpty(provider.apiKey))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(GetConfigurationHelpMessage(provider), MessageType.Warning);
            }
            else if (provider.verificationStatus == VerificationStatus.Checking)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Verifying API connection...", MessageType.Info);
            }
        }

        private void LoadProvidersFromJSON()
        {
            // Check if AIModels.json exists in user's Resources folder
            var resourcesPath = "Assets/Resources";
            var gameSmithPath = "Assets/Resources/GameSmith";
            var aiModelsPath = "Assets/Resources/GameSmith/AIModels.json";

            // Create directories if they don't exist
            if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!UnityEditor.AssetDatabase.IsValidFolder(gameSmithPath))
            {
                UnityEditor.AssetDatabase.CreateFolder(resourcesPath, "GameSmith");
            }

            // Create AIModels.json if it doesn't exist
            if (!System.IO.File.Exists(aiModelsPath))
            {
                CreateAIModelsFileFromDefaults();
                UnityEditor.AssetDatabase.Refresh();
            }

            // Now load the file
            TextAsset jsonFile = Resources.Load<TextAsset>("GameSmith/AIModels");

            if (jsonFile == null)
            {
                UnityEngine.Debug.LogError("[GameSmith] Failed to load AIModels.json after creation.");
                config.providers = CreateDefaultProviders();
                EditorUtility.SetDirty(config);
                return;
            }

            try
            {
                // Parse the JSON manually to extract provider configurations
                var loadedProviders = ParseProvidersJSON(jsonFile.text);

                if (loadedProviders.Count == 0)
                {
                    UnityEngine.Debug.LogWarning("[GameSmith] No providers found in providers.json");
                    return;
                }

                // Merge with existing providers to preserve API keys
                if (config.providers == null)
                {
                    config.providers = new System.Collections.Generic.List<ProviderConfiguration>();
                }

                foreach (var loadedProvider in loadedProviders)
                {
                    var existingProvider = config.providers.FirstOrDefault(p => p.providerName == loadedProvider.providerName);

                    if (existingProvider != null)
                    {
                        // Preserve the API key from existing config
                        loadedProvider.apiKey = existingProvider.apiKey;
                        loadedProvider.verificationStatus = existingProvider.verificationStatus;
                        loadedProvider.verificationMessage = existingProvider.verificationMessage;

                        // Replace the existing provider
                        int index = config.providers.IndexOf(existingProvider);
                        config.providers[index] = loadedProvider;
                    }
                    else
                    {
                        // Add new provider
                        config.providers.Add(loadedProvider);
                    }
                }

                // Remove providers that no longer exist in JSON
                var providersToRemove = config.providers
                    .Where(p => !loadedProviders.Any(lp => lp.providerName == p.providerName))
                    .ToList();

                foreach (var provider in providersToRemove)
                {
                    config.providers.Remove(provider);
                }

                EditorUtility.SetDirty(config);
                UnityEngine.Debug.Log($"[GameSmith] Loaded {loadedProviders.Count} providers from providers.json");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Error loading providers.json: {ex.Message}");
            }
        }

        private System.Collections.Generic.List<ProviderConfiguration> ParseProvidersJSON(string json)
        {
            var providers = new System.Collections.Generic.List<ProviderConfiguration>();

            try
            {
                // Find the providers array
                int providersArrayStart = json.IndexOf("\"providers\":");
                if (providersArrayStart == -1) return providers;

                int arrayStart = json.IndexOf("[", providersArrayStart);
                if (arrayStart == -1) return providers;

                // Parse each provider object
                int searchPos = arrayStart;
                while (true)
                {
                    int objStart = json.IndexOf("{", searchPos);
                    if (objStart == -1) break;

                    // Find the end of this object (naive - doesn't handle nested objects perfectly)
                    int objEnd = json.IndexOf("}", objStart);
                    if (objEnd == -1) break;

                    // Extract provider fields
                    string providerJson = json.Substring(objStart, objEnd - objStart + 1);

                    var provider = new ProviderConfiguration
                    {
                        providerName = ExtractJSONValue(providerJson, "name"),
                        apiUrl = ExtractJSONValue(providerJson, "apiUrl"),
                        apiKeyUrl = ExtractJSONValue(providerJson, "apiKeyUrl"),
                        models = new System.Collections.Generic.List<ModelConfiguration>()
                    };

                    // Parse models array
                    int modelsStart = providerJson.IndexOf("\"models\":");
                    if (modelsStart != -1)
                    {
                        int modelsArrayStart = providerJson.IndexOf("[", modelsStart);
                        int modelsArrayEnd = providerJson.LastIndexOf("]");

                        if (modelsArrayStart != -1 && modelsArrayEnd != -1)
                        {
                            string modelsJson = providerJson.Substring(modelsArrayStart, modelsArrayEnd - modelsArrayStart + 1);
                            provider.models = ParseModelsJSON(modelsJson);
                        }
                    }

                    providers.Add(provider);

                    searchPos = objEnd + 1;

                    // Check if we've reached the end of the providers array
                    int nextComma = json.IndexOf(",", searchPos);
                    int arrayEnd = json.IndexOf("]", searchPos);
                    if (arrayEnd != -1 && (nextComma == -1 || arrayEnd < nextComma))
                    {
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Error parsing providers JSON: {ex.Message}");
            }

            return providers;
        }

        private System.Collections.Generic.List<ModelConfiguration> ParseModelsJSON(string modelsJson)
        {
            var models = new System.Collections.Generic.List<ModelConfiguration>();

            try
            {
                int searchPos = 0;
                while (true)
                {
                    int objStart = modelsJson.IndexOf("{", searchPos);
                    if (objStart == -1) break;

                    int objEnd = modelsJson.IndexOf("}", objStart);
                    if (objEnd == -1) break;

                    string modelJson = modelsJson.Substring(objStart, objEnd - objStart + 1);

                    var model = new ModelConfiguration
                    {
                        modelName = ExtractJSONValue(modelJson, "id"),
                        displayName = ExtractJSONValue(modelJson, "displayName"),
                        isEnabled = true
                    };

                    models.Add(model);
                    searchPos = objEnd + 1;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Error parsing models JSON: {ex.Message}");
            }

            return models;
        }

        private string ExtractJSONValue(string json, string key)
        {
            string searchKey = $"\"{key}\":";
            int keyIndex = json.IndexOf(searchKey);
            if (keyIndex == -1) return "";

            int valueStart = json.IndexOf("\"", keyIndex + searchKey.Length);
            if (valueStart == -1) return "";

            valueStart++; // Move past the opening quote
            int valueEnd = json.IndexOf("\"", valueStart);
            if (valueEnd == -1) return "";

            return json.Substring(valueStart, valueEnd - valueStart);
        }

        private void CreateAIModelsFileFromDefaults()
        {
            string defaultContent = GetDefaultAIModelsJson();
            string filePath = "Assets/Resources/GameSmith/AIModels.json";

            try
            {
                System.IO.File.WriteAllText(filePath, defaultContent);
                UnityEngine.Debug.Log($"[GameSmith] Created AIModels.json at {filePath}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Failed to create AIModels.json: {ex.Message}");
            }
        }

        private string GetDefaultAIModelsJson()
        {
            // Try to load providers.json from the package as template
            TextAsset providersTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.sparkgames.gamesmith/Editor/providers.json");

            if (providersTemplate == null)
            {
                // Try local development path
                providersTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameSmith/UnityPackage/Editor/providers.json");
            }

            if (providersTemplate != null)
            {
                return providersTemplate.text;
            }

            // If no template found, use hardcoded defaults
            return @"{
  ""providers"": [
    {
      ""name"": ""Claude"",
      ""apiUrl"": ""https://api.anthropic.com/v1/messages"",
      ""apiKeyUrl"": ""https://console.anthropic.com/settings/keys"",
      ""models"": [
        { ""id"": ""claude-sonnet-4-20250514"", ""displayName"": ""Claude 4.5 Sonnet (Latest)"" },
        { ""id"": ""claude-opus-4-20250514"", ""displayName"": ""Claude 4.1 Opus"" },
        { ""id"": ""claude-3-5-sonnet-20241022"", ""displayName"": ""Claude 3.5 Sonnet"" },
        { ""id"": ""claude-3-opus-20240229"", ""displayName"": ""Claude 3 Opus"" },
        { ""id"": ""claude-3-haiku-20240307"", ""displayName"": ""Claude 3 Haiku"" }
      ]
    },
    {
      ""name"": ""Gemini"",
      ""apiUrl"": ""https://generativelanguage.googleapis.com/v1beta/models"",
      ""apiKeyUrl"": ""https://aistudio.google.com/app/apikey"",
      ""models"": [
        { ""id"": ""gemini-2.0-flash-exp"", ""displayName"": ""Gemini 2.5 Flash (Latest)"" },
        { ""id"": ""gemini-exp-1206"", ""displayName"": ""Gemini 2.5 Pro"" },
        { ""id"": ""gemini-1.5-pro"", ""displayName"": ""Gemini 1.5 Pro"" },
        { ""id"": ""gemini-1.5-flash"", ""displayName"": ""Gemini 1.5 Flash"" }
      ]
    },
    {
      ""name"": ""OpenRouter"",
      ""apiUrl"": ""https://openrouter.ai/api/v1/chat/completions"",
      ""apiKeyUrl"": ""https://openrouter.ai/keys"",
      ""models"": [
        { ""id"": ""anthropic/claude-sonnet-4-20250514"", ""displayName"": ""Claude 4.5 Sonnet"" },
        { ""id"": ""anthropic/claude-3.5-sonnet"", ""displayName"": ""Claude 3.5 Sonnet"" },
        { ""id"": ""openai/gpt-4o"", ""displayName"": ""GPT-4o"" },
        { ""id"": ""google/gemini-2.0-flash-exp:free"", ""displayName"": ""Gemini 2.5 Flash (Free)"" },
        { ""id"": ""meta-llama/llama-3.1-70b-instruct"", ""displayName"": ""Llama 3.1 70B"" },
        { ""id"": ""mistralai/mistral-large"", ""displayName"": ""Mistral Large"" }
      ]
    },
    {
      ""name"": ""OpenAI"",
      ""apiUrl"": ""https://api.openai.com/v1/chat/completions"",
      ""apiKeyUrl"": ""https://platform.openai.com/api-keys"",
      ""models"": [
        { ""id"": ""gpt-4o"", ""displayName"": ""GPT-4o (Latest)"" },
        { ""id"": ""gpt-4-turbo"", ""displayName"": ""GPT-4 Turbo"" },
        { ""id"": ""gpt-4"", ""displayName"": ""GPT-4"" },
        { ""id"": ""gpt-3.5-turbo"", ""displayName"": ""GPT-3.5 Turbo"" }
      ]
    },
    {
      ""name"": ""Ollama"",
      ""apiUrl"": ""http://localhost:11434/v1/chat/completions"",
      ""apiKeyUrl"": """",
      ""models"": [
        { ""id"": ""llama3.2:latest"", ""displayName"": ""Llama 3.2 (Latest)"" },
        { ""id"": ""llama3.1:latest"", ""displayName"": ""Llama 3.1"" },
        { ""id"": ""codellama:latest"", ""displayName"": ""Code Llama"" },
        { ""id"": ""mistral:latest"", ""displayName"": ""Mistral"" },
        { ""id"": ""mixtral:latest"", ""displayName"": ""Mixtral"" },
        { ""id"": ""qwen2.5-coder:latest"", ""displayName"": ""Qwen 2.5 Coder"" },
        { ""id"": ""deepseek-coder:latest"", ""displayName"": ""DeepSeek Coder"" },
        { ""id"": ""phi3:latest"", ""displayName"": ""Phi-3"" }
      ]
    }
  ]
}";
        }

        private System.Collections.Generic.List<ProviderConfiguration> CreateDefaultProviders()
        {
            // Parse the default JSON and return providers
            var jsonContent = GetDefaultAIModelsJson();
            return ParseProvidersJSON(jsonContent);
        }

        // Verification Helper Methods
        private Color GetStatusColor(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Verified:
                    return Color.green;
                case VerificationStatus.Checking:
                    return Color.yellow;
                case VerificationStatus.Failed:
                case VerificationStatus.NotConfigured:
                    return Color.red;
                default:
                    return Color.gray;
            }
        }

        private string GetStatusIcon(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Verified:
                    return "●"; // Filled circle
                case VerificationStatus.Checking:
                    return "◐"; // Half-filled circle
                case VerificationStatus.Failed:
                case VerificationStatus.NotConfigured:
                    return "●"; // Filled circle (will be colored red)
                default:
                    return "○"; // Empty circle
            }
        }

        private void UpdateVerificationStatus(ProviderConfiguration provider)
        {
            // Check if configuration has changed
            bool hasRequiredFields = !string.IsNullOrEmpty(provider.apiUrl);
            bool needsApiKey = !provider.providerName.ToLower().Contains("ollama");

            if (needsApiKey)
            {
                hasRequiredFields = hasRequiredFields && !string.IsNullOrEmpty(provider.apiKey);
            }

            // Only update to NotConfigured if not already checking or verified
            if (!hasRequiredFields && provider.verificationStatus != VerificationStatus.Checking)
            {
                provider.verificationStatus = VerificationStatus.NotConfigured;
                provider.verificationMessage = GetConfigurationHelpMessage(provider);
            }
        }

        private string GetConfigurationHelpMessage(ProviderConfiguration provider)
        {
            if (string.IsNullOrEmpty(provider.apiUrl))
            {
                return $"API URL is required for {provider.providerName}";
            }

            bool isOllama = provider.providerName.ToLower().Contains("ollama");
            if (!isOllama && string.IsNullOrEmpty(provider.apiKey))
            {
                if (provider.providerName == "Claude")
                {
                    return "API Key required. Get your key from: https://console.anthropic.com/account/keys";
                }
                else if (provider.providerName == "OpenAI")
                {
                    return "API Key required. Get your key from: https://platform.openai.com/api-keys";
                }
                else if (provider.providerName == "Gemini")
                {
                    return "API Key required. Get your key from: https://aistudio.google.com/app/apikey";
                }
                else if (provider.providerName == "OpenRouter")
                {
                    return "API Key required. Get your key from: https://openrouter.ai/keys";
                }
                else
                {
                    return $"API Key is required for {provider.providerName}";
                }
            }

            return "Enter API key and press Enter to verify";
        }

        private IEnumerator DelayedVerification(ProviderConfiguration provider, float delay)
        {
            yield return new EditorWaitForSeconds(delay);

            // Check if this is still the latest API key change
            string providerKey = $"{provider.providerName}_api_key";
            if (!lastApiKeyChangeTime.ContainsKey(providerKey))
                yield break;

            float timeSinceChange = Time.realtimeSinceStartup - lastApiKeyChangeTime[providerKey];
            if (timeSinceChange < delay - 0.1f)
                yield break;

            StartVerification(provider);
        }

        private void StartOllamaServer(ProviderConfiguration provider)
        {
            #if UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ollama serve",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            #elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"ollama serve\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            #endif

            // Wait a bit then try to connect
            var coroutine = new EditorCoroutine(WaitAndVerify(provider, 2.0f));
            coroutine.Start();
        }

        private IEnumerator WaitAndVerify(ProviderConfiguration provider, float delay)
        {
            yield return new EditorWaitForSeconds(delay);
            StartVerification(provider);
        }

        // Backwards-compat shim: some call sites still reference VerifyProviderAPI
        private void VerifyProviderAPI(ProviderConfiguration provider)
        {
            StartVerification(provider);
        }

        private void StartVerification(ProviderConfiguration provider)
        {
            // Avoid re-entrancy: if a recent verification is already running, skip
            if (provider.verificationStatus == VerificationStatus.Checking &&
                (Time.realtimeSinceStartup - provider.lastVerificationTime) < 15f)
            {
                return;
            }
            // Basic validation first
            if (string.IsNullOrEmpty(provider.apiUrl))
            {
                provider.verificationStatus = VerificationStatus.Failed;
                provider.verificationMessage = "API URL is required";
                Repaint();
                return;
            }

            bool isOllama = provider.providerName.ToLower().Contains("ollama");
            if (!isOllama && string.IsNullOrEmpty(provider.apiKey))
            {
                provider.verificationStatus = VerificationStatus.Failed;
                provider.verificationMessage = "API Key is required for " + provider.providerName;
                Repaint();
                return;
            }

            // Set status to checking (this will make the circle orange)
            provider.verificationStatus = VerificationStatus.Checking;
            provider.verificationMessage = "";
            provider.lastVerificationTime = Time.realtimeSinceStartup;
            Repaint();

            // Cancel any existing verification for this provider
            if (activeVerifications.ContainsKey(provider))
            {
                activeVerifications[provider]?.Stop();
                activeVerifications.Remove(provider);
            }

            // Start new verification coroutine
            var coroutine = new EditorCoroutine(VerifyAPIConnection(provider));
            coroutine.Start();
            activeVerifications[provider] = coroutine;
        }

        private IEnumerator VerifyAPIConnection(ProviderConfiguration provider)
        {
            bool isOllama = provider.providerName.ToLower().Contains("ollama");

            // Create appropriate test request based on provider
            UnityWebRequest request = null;

            try
            {
                if (isOllama)
            {
                // For Ollama, check if the server is running and fetch models
                string testUrl = provider.apiUrl.Replace("/v1/chat/completions", "/api/tags");
                request = UnityWebRequest.Get(testUrl);
                request.timeout = 5;

            }
            else if (provider.providerName == "Claude")
            {
                // For Claude, create a minimal test request
                request = new UnityWebRequest(provider.apiUrl, "POST");

                // Use the currently selected model for testing, or fallback to sonnet
                string testModel = config.selectedModel;
                if (string.IsNullOrEmpty(testModel) || !provider.models.Any(m => m.modelName == testModel))
                {
                    testModel = provider.models.FirstOrDefault()?.modelName ?? "claude-sonnet-4-20250514";
                }

                string testPayload = $@"{{
                    ""model"": ""{testModel}"",
                    ""messages"": [{{""role"": ""user"", ""content"": ""Hi""}}],
                    ""max_tokens"": 10
                }}";


                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(testPayload);
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", provider.apiKey);
                request.SetRequestHeader("anthropic-version", "2023-06-01");
                request.timeout = 10;
            }
            else if (provider.providerName == "OpenAI")
            {
                // For OpenAI, create a minimal test request
                request = new UnityWebRequest(provider.apiUrl, "POST");

                string testPayload = @"{
                    ""model"": ""gpt-3.5-turbo"",
                    ""messages"": [{""role"": ""user"", ""content"": ""Hi""}],
                    ""max_tokens"": 1
                }";

                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(testPayload);
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {provider.apiKey}");
                request.timeout = 10;
            }
            else if (provider.providerName == "Gemini")
            {
                // For Gemini, test with list models endpoint
                string testUrl = $"{provider.apiUrl}?key={provider.apiKey}";
                request = UnityWebRequest.Get(testUrl);
                request.timeout = 10;
            }
            else if (provider.providerName == "OpenRouter")
            {
                // For OpenRouter, create a minimal test request (same format as OpenAI)
                request = new UnityWebRequest(provider.apiUrl, "POST");

                // Use first model or fallback
                string testModel = provider.models.FirstOrDefault()?.modelName ?? "anthropic/claude-sonnet-4-20250514";

                string testPayload = $@"{{
                    ""model"": ""{testModel}"",
                    ""messages"": [{{""role"": ""user"", ""content"": ""Hi""}}],
                    ""max_tokens"": 10
                }}";


                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(testPayload);
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {provider.apiKey}");
                request.timeout = 10;
            }
            else
            {
                // Generic provider test
                request = UnityWebRequest.Get(provider.apiUrl);
                request.timeout = 5;
                if (!string.IsNullOrEmpty(provider.apiKey))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {provider.apiKey}");
                }
            }

                // Send the request
                yield return request.SendWebRequest();

                // Check if request was successful
                bool isSuccess = request.result == UnityWebRequest.Result.Success &&
                               request.responseCode >= 200 && request.responseCode < 300;

                if (isSuccess)
                {
                    // For Ollama, parse the models from the response
                    if (isOllama)
                    {
                        try
                        {
                            string responseText = request.downloadHandler.text;
                            // Parse the JSON response to extract model names
                            var models = ParseOllamaModels(responseText);

                            if (models.Count > 0)
                            {
                                provider.models.Clear();
                                provider.models.AddRange(models);
                            }
                            else
                            {
                                provider.verificationStatus = VerificationStatus.Failed;
                                provider.verificationMessage = "No Ollama models found. Run 'ollama pull <model>' to install models.";
                                yield break;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            provider.verificationStatus = VerificationStatus.Failed;
                            provider.verificationMessage = $"Failed to parse Ollama models: {ex.Message}";
                            yield break;
                        }
                    }

                    provider.verificationStatus = VerificationStatus.Verified;
                    provider.verificationMessage = "";
                }
            else if (request.responseCode == 401)
            {
                // Authentication error
                provider.verificationStatus = VerificationStatus.Failed;
                provider.verificationMessage = "Invalid API key. Please check your API key.";
            }
            else
            {
                provider.verificationStatus = VerificationStatus.Failed;

                // Parse error message
                string errorDetail = "";
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    if (isOllama)
                    {
                        errorDetail = "Cannot connect to Ollama. Make sure Ollama is running (ollama serve).";
                    }
                    else
                    {
                        errorDetail = "Connection error. Check your internet connection and API URL.";
                    }
                }
                else if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    switch (request.responseCode)
                    {
                        case 401:
                            errorDetail = "Authentication failed. Check your API key.";
                            break;
                        case 403:
                            errorDetail = "Access forbidden. Check your API key permissions.";
                            break;
                        case 404:
                            errorDetail = "API endpoint not found. Check the API URL.";
                            break;
                        case 429:
                            errorDetail = "Rate limited. Too many requests.";
                            break;
                        default:
                            errorDetail = $"HTTP Error {request.responseCode}: {request.error}";
                            break;
                    }
                }
                else
                {
                    errorDetail = request.error;
                }

                provider.verificationMessage = errorDetail;
            }
            }
            finally
            {
                // Ensure request is always disposed
                if (request != null)
                {
                    request.Dispose();
                    request = null;
                }
                // Remove from active map to prevent stuck processes
                if (activeVerifications.ContainsKey(provider))
                {
                    activeVerifications[provider]?.Stop();
                    activeVerifications.Remove(provider);
                }
            }
        }

        private System.Collections.Generic.List<ModelConfiguration> ParseOllamaModels(string jsonResponse)
        {
            var models = new System.Collections.Generic.List<ModelConfiguration>();

            try
            {
                // Simple JSON parsing - look for model names
                // Ollama returns: {"models":[{"name":"llama2:latest",...},{"name":"codellama:latest",...}]}

                int modelsArrayStart = jsonResponse.IndexOf("\"models\":");
                if (modelsArrayStart == -1) return models;

                // Find all "name" fields within the models array
                int searchPos = modelsArrayStart;
                while (true)
                {
                    int nameStart = jsonResponse.IndexOf("\"name\":\"", searchPos);
                    if (nameStart == -1) break;

                    nameStart += 8; // Move past "name":"
                    int nameEnd = jsonResponse.IndexOf("\"", nameStart);
                    if (nameEnd == -1) break;

                    string modelName = jsonResponse.Substring(nameStart, nameEnd - nameStart);

                    // Clean up the model name (remove :latest, :13b, etc. tags for display)
                    string displayName = modelName;
                    if (displayName.Contains(":"))
                    {
                        var parts = displayName.Split(':');
                        displayName = parts[0];

                        // Capitalize first letter
                        if (displayName.Length > 0)
                        {
                            displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
                        }

                        // Add size info if available
                        if (parts.Length > 1 && parts[1] != "latest")
                        {
                            displayName += $" ({parts[1]})";
                        }
                    }

                    models.Add(new ModelConfiguration
                    {
                        modelName = modelName,
                        displayName = displayName,
                        isEnabled = true
                    });

                    searchPos = nameEnd;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GameSmith] Error parsing Ollama models: {ex.Message}");
            }

            return models;
        }

        private void DrawStatusIndicator(ProviderConfiguration provider)
        {
            if (provider == null)
            {
                GUI.color = Color.gray;
                GUILayout.Label("●", GUILayout.Width(20));
                GUI.color = Color.white;
                return;
            }

            Color statusColor;
            switch (provider.verificationStatus)
            {
                case VerificationStatus.Verified:
                    statusColor = Color.green;
                    break;
                case VerificationStatus.Checking:
                    statusColor = new Color(1f, 0.5f, 0f); // Orange
                    break;
                case VerificationStatus.Failed:
                    statusColor = Color.red;
                    break;
                default:
                    statusColor = Color.red;
                    break;
            }

            GUI.color = statusColor;
            GUILayout.Label("●", GUILayout.Width(20));
            GUI.color = Color.white;
        }

        private string GetOllamaStatusText(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Verified:
                    return "Connected";
                case VerificationStatus.Checking:
                    return "Connecting...";
                case VerificationStatus.Failed:
                    return "Disconnected";
                default:
                    return "Not Connected";
            }
        }

        private Color GetStatusLabelColor(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Verified:
                    return new Color(0.3f, 0.8f, 0.3f);
                case VerificationStatus.Checking:
                    return new Color(0.9f, 0.7f, 0.3f);
                case VerificationStatus.Failed:
                    return new Color(0.9f, 0.3f, 0.3f);
                default:
                    return Color.gray;
            }
        }

        private string GetActionButtonLabel(ProviderConfiguration provider)
        {
            if (provider == null) return "Verify";

            bool isOllama = provider.providerName.ToLower().Contains("ollama");

            if (isOllama)
            {
                return provider.verificationStatus == VerificationStatus.Verified ? "Refresh Models" : "Connect";
            }

            switch (provider.verificationStatus)
            {
                case VerificationStatus.Verified:
                    return "✓ Verified";
                case VerificationStatus.Checking:
                    return "Verifying...";
                default:
                    return "Verify Connection";
            }
        }

        private void SelectAIModelsAsset()
        {
            // Select AIModels.json in project
            var aiModelsAsset = Resources.Load<TextAsset>("GameSmith/AIModels");

            if (aiModelsAsset != null)
            {
                Selection.activeObject = aiModelsAsset;
                EditorGUIUtility.PingObject(aiModelsAsset);
                UnityEngine.Debug.Log("[GameSmith] Selected AIModels.json in Project");
            }
            else
            {
                // File doesn't exist - offer to create it
                bool createFile = EditorUtility.DisplayDialog(
                    "AIModels.json Not Found",
                    "AIModels.json doesn't exist yet. Would you like to create it with default providers?\n\n" +
                    "Location: Assets/Resources/GameSmith/AIModels.json",
                    "Create",
                    "Cancel"
                );

                if (createFile)
                {
                    GameSmithConfig.ResetAIModelsToDefault();

                    // Try to select it after creation
                    UnityEditor.AssetDatabase.Refresh();
                    aiModelsAsset = Resources.Load<TextAsset>("GameSmith/AIModels");
                    if (aiModelsAsset != null)
                    {
                        Selection.activeObject = aiModelsAsset;
                        EditorGUIUtility.PingObject(aiModelsAsset);
                    }
                }
            }
        }

        private void ResetModelParametersToDefaults()
        {
            config.temperature = 0.7f; // Default temperature
            config.maxTokens = 4096;   // Default max tokens
            EditorUtility.SetDirty(config);
            UnityEngine.Debug.Log("[GameSmith] Model parameters reset to defaults.");
        }
    }

    /// <summary>
    /// Helper class to run coroutines in the Unity Editor
    /// </summary>
    public class EditorCoroutine
    {
        private IEnumerator routine;
        private bool isRunning = false;

        public EditorCoroutine(IEnumerator routine)
        {
            this.routine = routine;
        }

        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                EditorApplication.update += Update;
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;
                EditorApplication.update -= Update;
            }
        }

        private void Update()
        {
            if (!isRunning) return;

            try
            {
                if (!routine.MoveNext())
                {
                    Stop();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"EditorCoroutine error: {ex}");
                Stop();
            }
        }
    }

    /// <summary>
    /// Helper class to wait for seconds in Editor coroutines
    /// </summary>
    public class EditorWaitForSeconds : IEnumerator
    {
        private float waitTime;
        private float startTime;

        public EditorWaitForSeconds(float time)
        {
            waitTime = time;
            startTime = Time.realtimeSinceStartup;
        }

        public object Current => null;

        public bool MoveNext()
        {
            return (Time.realtimeSinceStartup - startTime) < waitTime;
        }

        public void Reset()
        {
            startTime = Time.realtimeSinceStartup;
        }
    }
}