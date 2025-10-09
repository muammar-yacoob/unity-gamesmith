using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// ScriptableObject to store AI API configuration
    /// </summary>
    [CreateAssetMenu(fileName = "GameSmithConfig", menuName = "Game Smith/Config")]
    public class GameSmithConfig : ScriptableObject
    {
        [Header("AI Provider Settings")]
        [Tooltip("AI provider name (e.g., Ollama, OpenAI, Anthropic)")]
        public string providerName = "Ollama";

        [Tooltip("API endpoint URL")]
        public string apiUrl = "http://localhost:11434/v1/chat/completions";

        [Tooltip("Your API key (not required for Ollama)")]
        public string apiKey = "";

        [Tooltip("Available models (comma-separated, e.g., llama2,codellama,mistral)")]
        public string availableModels = "llama2";

        [Tooltip("Currently selected model")]
        public string selectedModel = "llama2";

        [Header("Generation Settings")]
        [Range(0f, 2f)]
        [Tooltip("Higher values make output more random")]
        public float temperature = 0.7f;

        [Range(1, 4096)]
        [Tooltip("Maximum tokens in response")]
        public int maxTokens = 2000;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(apiKey);
        }

        public string GetValidationMessage()
        {
            if (string.IsNullOrEmpty(apiUrl)) return "API URL is required";
            if (string.IsNullOrEmpty(apiKey)) return "API Key is required";
            return "Configuration is valid";
        }

        public List<string> GetModelsList()
        {
            if (string.IsNullOrEmpty(availableModels))
                return new List<string> { "llama2" };

            return availableModels
                .Split(',')
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();
        }

        public string GetCurrentModel()
        {
            return string.IsNullOrEmpty(selectedModel) ? "llama2" : selectedModel;
        }

        /// <summary>
        /// Get or create the default config instance
        /// </summary>
        public static GameSmithConfig GetOrCreate()
        {
            var config = Resources.Load<GameSmithConfig>("GameSmithConfig");
            if (config == null)
            {
                config = CreateInstance<GameSmithConfig>();
#if UNITY_EDITOR
                var resourcesPath = "Assets/Resources";
                if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                }
                UnityEditor.AssetDatabase.CreateAsset(config, $"{resourcesPath}/GameSmithConfig.asset");
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }
            return config;
        }
    }
}
