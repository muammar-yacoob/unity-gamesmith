using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple settings window for GameSmith configuration
    /// </summary>
    public class GameSmithSettingsWindow : EditorWindow
    {
        private GameSmithConfig config;
        private Vector2 scrollPosition;

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
            
            EditorGUILayout.Space(10);
            
            // Model Parameters
            DrawParametersSection();
            
            EditorGUILayout.Space(10);
            
            // Help
            DrawHelpSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawProviderSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("AI Provider", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Provider dropdown
            var providerNames = config.GetProviderNames().ToArray();
            int currentProviderIndex = System.Array.IndexOf(providerNames, config.activeProvider);
            if (currentProviderIndex < 0 && providerNames.Length > 0)
            {
                currentProviderIndex = 0;
                config.activeProvider = providerNames[0];
            }

            EditorGUI.BeginChangeCheck();
            int newProviderIndex = EditorGUILayout.Popup("Provider", currentProviderIndex, providerNames);
            if (EditorGUI.EndChangeCheck() && newProviderIndex >= 0)
            {
                config.activeProvider = providerNames[newProviderIndex];
                // Reset to first model when changing provider
                var models = config.GetModelsList();
                if (models.Count > 0)
                {
                    config.selectedModel = models[0];
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
                EditorGUI.BeginChangeCheck();
                string newKey = EditorGUILayout.PasswordField("API Key", currentKey);
                if (EditorGUI.EndChangeCheck())
                {
                    config.SetApiKey(config.activeProvider, newKey);
                }

                EditorGUILayout.Space(3);
                
                var apiKeyUrl = config.GetApiKeyUrl();
                if (!string.IsNullOrEmpty(apiKeyUrl))
                {
                    if (GUILayout.Button("Get API Key", GUILayout.Height(24)))
                    {
                        Application.OpenURL(apiKeyUrl);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Ollama runs locally. No API key needed.\nMake sure Ollama is running: ollama serve", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawParametersSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Model Parameters", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.temperature = EditorGUILayout.Slider("Temperature", config.temperature, 0f, 2f);
            config.maxTokens = EditorGUILayout.IntSlider("Max Tokens", config.maxTokens, 256, 8192);

            EditorGUILayout.EndVertical();
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Status: " + (config.IsValid() ? "✓ Configured" : "⚠ Not Configured"), EditorStyles.miniLabel);
            
            if (!config.IsValid())
            {
                EditorGUILayout.HelpBox(config.GetValidationMessage(), MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Open Chat Window", GUILayout.Height(28)))
            {
                GameSmithWindow.ShowWindow();
                Close();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
