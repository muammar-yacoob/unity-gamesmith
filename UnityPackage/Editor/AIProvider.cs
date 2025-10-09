using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// ScriptableObject that stores configuration for a specific AI provider
    /// </summary>
    [CreateAssetMenu(fileName = "New AI Provider", menuName = "Game Smith/AI Provider", order = 1)]
    public class AIProvider : ScriptableObject
    {
        [Header("Provider Information")]
        [Tooltip("Display name for the provider (e.g., 'Ollama - CodeLlama', 'Claude Sonnet 4')")]
        public string providerName = "New Provider";

        [Tooltip("Short description of this provider")]
        [TextArea(2, 3)]
        public string description = "";

        [Header("API Configuration")]
        [Tooltip("API endpoint URL")]
        public string apiUrl = "";

        [Tooltip("Model name/identifier")]
        public string model = "";

        [Tooltip("API key (leave empty for local providers like Ollama)")]
        public string apiKey = "";

        [Header("Settings")]
        [Tooltip("Request timeout in seconds")]
        [Range(30, 300)]
        public int timeout = 120;

        [Tooltip("Temperature (creativity) - Lower = more focused, Higher = more creative")]
        [Range(0f, 1f)]
        public float temperature = 0.7f;

        [Header("Provider Type")]
        [Tooltip("Is this a local provider (like Ollama) or cloud-based?")]
        public bool isLocal = false;

        [Tooltip("Does this provider require an API key?")]
        public bool requiresApiKey = true;

        /// <summary>
        /// Creates a config from this provider
        /// </summary>
        public AIAgentConfig ToConfig()
        {
            return new AIAgentConfig
            {
                apiUrl = this.apiUrl,
                model = this.model,
                apiKey = this.apiKey,
                timeout = this.timeout,
                temperature = this.temperature
            };
        }

        /// <summary>
        /// Validates if this provider is properly configured
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(apiUrl)) return false;
            if (string.IsNullOrWhiteSpace(model)) return false;
            if (requiresApiKey && string.IsNullOrWhiteSpace(apiKey)) return false;
            return true;
        }

        /// <summary>
        /// Gets a user-friendly validation message
        /// </summary>
        public string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(apiUrl))
                return "API URL is required";
            if (string.IsNullOrWhiteSpace(model))
                return "Model name is required";
            if (requiresApiKey && string.IsNullOrWhiteSpace(apiKey))
                return "API Key is required for this provider";
            return "Configuration is valid";
        }
    }
}
