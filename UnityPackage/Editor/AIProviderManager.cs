using UnityEngine;
using UnityEditor;
using System.IO;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Manages AI providers and initializes defaults
    /// </summary>
    public static class AIProviderManager
    {
        private const string RESOURCES_PATH = "Assets/Resources/GameSmith";
        private const string DATABASE_PATH = RESOURCES_PATH + "/AIProviderDatabase.asset";
        private const string PROVIDERS_PATH = RESOURCES_PATH + "/Providers";

        private static AIProviderDatabase _database;

        /// <summary>
        /// Gets or creates the provider database
        /// </summary>
        public static AIProviderDatabase GetDatabase()
        {
            if (_database != null)
                return _database;

            // Try to load from Resources
            _database = Resources.Load<AIProviderDatabase>("GameSmith/AIProviderDatabase");

            if (_database == null)
            {
                // Create default database
                _database = CreateDefaultDatabase();
            }

            return _database;
        }

        /// <summary>
        /// Creates the default provider database with common providers
        /// </summary>
        private static AIProviderDatabase CreateDefaultDatabase()
        {
            // Ensure directories exist
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/GameSmith"))
                AssetDatabase.CreateFolder("Assets/Resources", "GameSmith");
            if (!AssetDatabase.IsValidFolder(PROVIDERS_PATH))
                AssetDatabase.CreateFolder(RESOURCES_PATH, "Providers");

            // Create database
            var database = ScriptableObject.CreateInstance<AIProviderDatabase>();
            database.providers = new System.Collections.Generic.List<AIProvider>();

            // Create default providers
            database.providers.Add(CreateOllamaProvider());
            database.providers.Add(CreateClaudeProvider());
            database.providers.Add(CreateOpenAIGPT4Provider());
            database.providers.Add(CreateOpenAIGPT35Provider());
            database.providers.Add(CreateLMStudioProvider());

            database.defaultProviderIndex = 0; // Ollama as default

            AssetDatabase.CreateAsset(database, DATABASE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created AI Provider Database at {DATABASE_PATH}");

            return database;
        }

        private static AIProvider CreateOllamaProvider()
        {
            var provider = ScriptableObject.CreateInstance<AIProvider>();
            provider.providerName = "Ollama - CodeLlama (Local, Free)";
            provider.description = "Local AI with CodeLlama model. Fast and free, runs on your machine.";
            provider.apiUrl = "http://localhost:11434/api/generate";
            provider.model = "codellama";
            provider.apiKey = "";
            provider.timeout = 120;
            provider.temperature = 0.7f;
            provider.isLocal = true;
            provider.requiresApiKey = false;

            AssetDatabase.CreateAsset(provider, $"{PROVIDERS_PATH}/Ollama_CodeLlama.asset");
            return provider;
        }

        private static AIProvider CreateClaudeProvider()
        {
            var provider = ScriptableObject.CreateInstance<AIProvider>();
            provider.providerName = "Claude Sonnet 4 (Recommended)";
            provider.description = "Best quality AI for code generation. Requires API key from Anthropic.";
            provider.apiUrl = "https://api.anthropic.com/v1/messages";
            provider.model = "claude-sonnet-4";
            provider.apiKey = "";
            provider.timeout = 120;
            provider.temperature = 0.7f;
            provider.isLocal = false;
            provider.requiresApiKey = true;

            AssetDatabase.CreateAsset(provider, $"{PROVIDERS_PATH}/Claude_Sonnet4.asset");
            return provider;
        }

        private static AIProvider CreateOpenAIGPT4Provider()
        {
            var provider = ScriptableObject.CreateInstance<AIProvider>();
            provider.providerName = "OpenAI GPT-4";
            provider.description = "High quality AI from OpenAI. Requires API key.";
            provider.apiUrl = "https://api.openai.com/v1/chat/completions";
            provider.model = "gpt-4";
            provider.apiKey = "";
            provider.timeout = 120;
            provider.temperature = 0.7f;
            provider.isLocal = false;
            provider.requiresApiKey = true;

            AssetDatabase.CreateAsset(provider, $"{PROVIDERS_PATH}/OpenAI_GPT4.asset");
            return provider;
        }

        private static AIProvider CreateOpenAIGPT35Provider()
        {
            var provider = ScriptableObject.CreateInstance<AIProvider>();
            provider.providerName = "OpenAI GPT-3.5 Turbo";
            provider.description = "Fast and cost-effective AI from OpenAI. Requires API key.";
            provider.apiUrl = "https://api.openai.com/v1/chat/completions";
            provider.model = "gpt-3.5-turbo";
            provider.apiKey = "";
            provider.timeout = 120;
            provider.temperature = 0.7f;
            provider.isLocal = false;
            provider.requiresApiKey = true;

            AssetDatabase.CreateAsset(provider, $"{PROVIDERS_PATH}/OpenAI_GPT35Turbo.asset");
            return provider;
        }

        private static AIProvider CreateLMStudioProvider()
        {
            var provider = ScriptableObject.CreateInstance<AIProvider>();
            provider.providerName = "LM Studio (Local, Free)";
            provider.description = "Local AI server. Free and runs on your machine.";
            provider.apiUrl = "http://localhost:1234/v1/chat/completions";
            provider.model = "local-model";
            provider.apiKey = "";
            provider.timeout = 120;
            provider.temperature = 0.7f;
            provider.isLocal = true;
            provider.requiresApiKey = false;

            AssetDatabase.CreateAsset(provider, $"{PROVIDERS_PATH}/LMStudio.asset");
            return provider;
        }

        /// <summary>
        /// Saves the current provider selection
        /// </summary>
        public static void SaveProviderSelection(string providerName)
        {
            EditorPrefs.SetString("GameSmith_SelectedProvider", providerName);
        }

        /// <summary>
        /// Loads the saved provider selection
        /// </summary>
        public static string LoadProviderSelection()
        {
            return EditorPrefs.GetString("GameSmith_SelectedProvider", "");
        }

        /// <summary>
        /// Gets the currently selected provider
        /// </summary>
        public static AIProvider GetSelectedProvider()
        {
            var database = GetDatabase();
            if (database == null) return null;

            string savedName = LoadProviderSelection();
            if (!string.IsNullOrEmpty(savedName))
            {
                var provider = database.GetProviderByName(savedName);
                if (provider != null) return provider;
            }

            return database.GetDefaultProvider();
        }

        /// <summary>
        /// Updates the API key for a provider
        /// </summary>
        public static void UpdateProviderApiKey(AIProvider provider, string newApiKey)
        {
            if (provider == null) return;

            provider.apiKey = newApiKey;
            EditorUtility.SetDirty(provider);
            AssetDatabase.SaveAssets();
        }
    }
}
