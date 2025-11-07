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

        // Load providers from AIModels.json in package Resources
        public void LoadProviders()
        {
            try
            {
                // Load from package Resources folder
                var aiModelsAsset = Resources.Load<TextAsset>("GameSmith/AIModels");

                if (aiModelsAsset == null)
                {
                    Debug.LogError("[GameSmith] AIModels.json not found in package Resources/GameSmith/. Please ensure the package is properly installed.");
                    return;
                }

                string json = aiModelsAsset.text;
                var data = JsonUtility.FromJson<ProvidersJson>(json);

                if (data != null && data.providers != null)
                {
                    providers = data.providers;

                    // Count total models across all providers (excluding Ollama which loads dynamically)
                    int totalModels = providers.Where(p => p.name != "Ollama").Sum(p => p.models?.Count ?? 0);
                    Debug.Log($"Game Smith 🗡️ ready with {totalModels} AI models. Alt+G to configure");
                }
                else
                {
                    Debug.LogError("[GameSmith] Failed to parse AIModels.json - invalid format");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSmith] Failed to load AIModels.json: {e.Message}");
            }
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

            // For Ollama, dynamically fetch models if list is empty
            if (provider.name == "Ollama" && provider.models.Count == 0)
            {
                RefreshOllamaModels();
            }

            return provider.models
                .Where(m => m.isEnabled)
                .Select(m => m.id)
                .ToList();
        }

        // Refresh Ollama models from local server (async)
        public void RefreshOllamaModels()
        {
            EditorCoroutineRunner.StartCoroutine(RefreshOllamaModelsAsync());
        }

        private System.Collections.IEnumerator RefreshOllamaModelsAsync()
        {
            var provider = providers.FirstOrDefault(p => p.name == "Ollama");
            if (provider == null) yield break;

            UnityEngine.Networking.UnityWebRequest request = null;
            try
            {
                request = UnityEngine.Networking.UnityWebRequest.Get("http://localhost:11434/api/tags");
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var response = request.downloadHandler.text;
                    var data = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;

                    if (data != null && data.ContainsKey("models"))
                    {
                        var models = data["models"] as List<object>;
                        if (models != null)
                        {
                            provider.models.Clear();
                            foreach (var model in models)
                            {
                                var modelDict = model as Dictionary<string, object>;
                                if (modelDict != null && modelDict.ContainsKey("name"))
                                {
                                    string modelName = modelDict["name"].ToString();
                                    provider.models.Add(new ModelJson
                                    {
                                        id = modelName,
                                        displayName = modelName,
                                        isEnabled = true
                                    });
                                }
                            }
                            Debug.Log($"Game Smith 🗡️ detected {provider.models.Count} Ollama models");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Game Smith 🗡️ Ollama server not running. Start with: ollama serve");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Game Smith 🗡️ Could not connect to Ollama: {ex.Message}");
            }
            finally
            {
                request?.Dispose();
            }
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
