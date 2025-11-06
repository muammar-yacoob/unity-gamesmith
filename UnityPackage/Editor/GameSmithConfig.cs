using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple helper class that loads provider configurations from AIModels.json
    /// and provides convenient access to settings stored in GameSmithSettings
    /// </summary>
    public class GameSmithConfig
    {
        [Serializable]
        public class ModelJson
        {
            public string id;
            public string displayName;
            public bool isEnabled = true;
        }

        [Serializable]
        public class ProviderJson
        {
            public string name;
            public string apiUrl;
            public string apiKeyUrl;
            public List<ModelJson> models;
        }

        [Serializable]
        public class ProvidersJson
        {
            public List<ProviderJson> providers;
        }

        // Singleton instance
        private static GameSmithConfig instance;
        public static GameSmithConfig GetOrCreate()
        {
            if (instance == null)
            {
                instance = new GameSmithConfig();
                instance.LoadProviders();
            }
            return instance;
        }

        // Provider data loaded from JSON
        private List<ProviderJson> providers = new List<ProviderJson>();

        // Properties that delegate to GameSmithSettings
        public string activeProvider
        {
            get => GameSmithSettings.Instance.activeProvider;
            set
            {
                GameSmithSettings.Instance.activeProvider = value;
                GameSmithSettings.Instance.Save();
            }
        }

        public string selectedModel
        {
            get => GameSmithSettings.Instance.selectedModel;
            set
            {
                GameSmithSettings.Instance.selectedModel = value;
                GameSmithSettings.Instance.Save();
            }
        }

        public float temperature
        {
            get => GameSmithSettings.Instance.temperature;
            set
            {
                GameSmithSettings.Instance.temperature = value;
                GameSmithSettings.Instance.Save();
            }
        }

        public int maxTokens
        {
            get => GameSmithSettings.Instance.maxTokens;
            set
            {
                GameSmithSettings.Instance.maxTokens = value;
                GameSmithSettings.Instance.Save();
            }
        }

        // Convenience properties
        public string apiUrl => GetActiveProviderData()?.apiUrl ?? "";
        public string apiKey => GameSmithSettings.Instance.GetApiKey(activeProvider);
        
        public void SetApiKey(string provider, string key)
        {
            GameSmithSettings.Instance.SetApiKey(provider, key);
        }

        // Load providers from AIModels.json
        public void LoadProviders()
        {
            // First, ensure AIModels.json exists in user's Assets/GameSmith/
            string userGameSmithPath = "Assets/GameSmith";
            string userModelsPath = System.IO.Path.Combine(userGameSmithPath, "AIModels.json");

            if (!System.IO.File.Exists(userModelsPath))
            {
                // Create default AIModels.json in user's project
                CreateDefaultAIModels(userGameSmithPath, userModelsPath);
            }

            // Load directly from file
            if (!System.IO.File.Exists(userModelsPath))
            {
                Debug.LogError("[GameSmith] AIModels.json not found in Assets/GameSmith/");
                return;
            }

            try
            {
                string json = System.IO.File.ReadAllText(userModelsPath);
                var data = JsonUtility.FromJson<ProvidersJson>(json);
                if (data != null && data.providers != null)
                {
                    providers = data.providers;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSmith] Failed to parse AIModels.json: {e.Message}");
            }
        }

        private void CreateDefaultAIModels(string resourcesPath, string modelsPath)
        {
            // Create directory if it doesn't exist
            if (!System.IO.Directory.Exists(resourcesPath))
            {
                System.IO.Directory.CreateDirectory(resourcesPath);
            }

            // Default AI models configuration
            string defaultJson = @"{
  ""providers"": [
    {
      ""name"": ""Claude"",
      ""apiUrl"": ""https://api.anthropic.com/v1/messages"",
      ""apiKeyUrl"": ""https://console.anthropic.com/settings/keys"",
      ""models"": [
        { ""id"": ""claude-3-5-sonnet-20241022"", ""displayName"": ""Claude 3.5 Sonnet"", ""isEnabled"": true }
      ]
    },
    {
      ""name"": ""OpenAI"",
      ""apiUrl"": ""https://api.openai.com/v1/chat/completions"",
      ""apiKeyUrl"": ""https://platform.openai.com/api-keys"",
      ""models"": [
        { ""id"": ""gpt-4o"", ""displayName"": ""GPT-4o"", ""isEnabled"": true }
      ]
    },
    {
      ""name"": ""Gemini"",
      ""apiUrl"": ""https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent"",
      ""apiKeyUrl"": ""https://aistudio.google.com/app/apikey"",
      ""models"": [
        { ""id"": ""gemini-2.0-flash-exp"", ""displayName"": ""Gemini 2.0 Flash (Exp)"", ""isEnabled"": true },
        { ""id"": ""gemini-1.5-pro"", ""displayName"": ""Gemini 1.5 Pro"", ""isEnabled"": true },
        { ""id"": ""gemini-1.5-flash"", ""displayName"": ""Gemini 1.5 Flash"", ""isEnabled"": true }
      ]
    },
    {
      ""name"": ""Ollama"",
      ""apiUrl"": ""http://localhost:11434/v1/chat/completions"",
      ""apiKeyUrl"": """",
      ""models"": [
        { ""id"": ""qwen3:8b"", ""displayName"": ""Qwen3 8B"", ""isEnabled"": true },
        { ""id"": ""deepseek-coder-v2:16b"", ""displayName"": ""DeepSeek Coder V2 16B"", ""isEnabled"": true }
      ]
    }
  ]
}";

            System.IO.File.WriteAllText(modelsPath, defaultJson);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"[GameSmith] Created default AIModels.json at {modelsPath}");
        }

        // Get list of provider names
        public List<string> GetProviderNames()
        {
            return providers.Select(p => p.name).ToList();
        }

        // Get active provider data
        public ProviderJson GetActiveProviderData()
        {
            return providers.FirstOrDefault(p => p.name == activeProvider);
        }

        // Get models for active provider
        public List<string> GetModelsList()
        {
            var provider = GetActiveProviderData();
            if (provider == null || provider.models == null) return new List<string>();
            
            return provider.models
                .Where(m => m.isEnabled)
                .Select(m => m.id)
                .ToList();
        }

        // Get display name for a model
        public string GetModelDisplayName(string modelId)
        {
            var provider = GetActiveProviderData();
            if (provider == null) return modelId;
            
            var model = provider.models?.FirstOrDefault(m => m.id == modelId);
            return model?.displayName ?? modelId;
        }

        // Get current model or first available
        public string GetCurrentModel()
        {
            var models = GetModelsList();
            if (models.Count == 0) return "default";
            
            if (!string.IsNullOrEmpty(selectedModel) && models.Contains(selectedModel))
                return selectedModel;
            
            return models[0];
        }

        // Get API key URL for active provider
        public string GetApiKeyUrl()
        {
            return GetActiveProviderData()?.apiKeyUrl ?? "";
        }

        // Simple validation
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(apiUrl)) return false;
            
            // Ollama doesn't need API key
            if (activeProvider.ToLower().Contains("ollama")) return true;
            
            return !string.IsNullOrEmpty(apiKey);
        }

        public string GetValidationMessage()
        {
            if (string.IsNullOrEmpty(apiUrl))
                return "API URL not configured";
            
            if (string.IsNullOrEmpty(apiKey) && !activeProvider.ToLower().Contains("ollama"))
                return $"{activeProvider} API key not configured";
            
            return "Configuration valid";
        }

        // Get system prompt (simple Unity context)
        public string GetSystemPrompt()
        {
            return "You are a helpful AI assistant for Unity game development. " +
                   "Provide clear, concise answers about Unity, C#, and game development.";
        }
    }
}
