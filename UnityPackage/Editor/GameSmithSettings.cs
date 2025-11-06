using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// User settings stored in JSON (API keys, preferences)
    /// Stored in ProjectSettings/GameSmithSettings.json (not version controlled)
    /// </summary>
    [Serializable]
    public class GameSmithSettings
    {
        public string activeProvider = "Claude";
        public string selectedModel = "claude-sonnet-4-20250514";
        public float temperature = 0.7f;
        public int maxTokens = 512;
        public string rulesAssetPath = "";

        [Serializable]
        public class ApiKeyEntry
        {
            public string provider;
            public string apiKey;
            public bool isVerified = false;
        }

        public List<ApiKeyEntry> apiKeys = new List<ApiKeyEntry>();

        private static string SettingsPath => Path.Combine("ProjectSettings", "GameSmithSettings.json");
        private static GameSmithSettings _instance;

        public static GameSmithSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public static GameSmithSettings Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonUtility.FromJson<GameSmithSettings>(json);
                    Debug.Log("[GameSmith] Settings loaded from " + SettingsPath);
                    return settings;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GameSmith] Failed to load settings: {e.Message}");
                }
            }

            // First time - create with defaults
            Debug.Log("[GameSmith] Creating default settings at " + SettingsPath);
            var newSettings = new GameSmithSettings();
            newSettings.Save();
            return newSettings;
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSmith] Failed to save settings: {e.Message}");
            }
        }

        public string GetApiKey(string providerName)
        {
            var entry = apiKeys.Find(k => k.provider == providerName);
            return entry?.apiKey ?? "";
        }

        public void SetApiKey(string providerName, string apiKey)
        {
            var entry = apiKeys.Find(k => k.provider == providerName);
            if (entry != null)
            {
                entry.apiKey = apiKey;
                // Reset verification when key changes
                entry.isVerified = false;
            }
            else
            {
                apiKeys.Add(new ApiKeyEntry { provider = providerName, apiKey = apiKey, isVerified = false });
            }
            Save();
        }

        public bool IsApiKeyVerified(string providerName)
        {
            var entry = apiKeys.Find(k => k.provider == providerName);
            return entry?.isVerified ?? false;
        }

        public void SetApiKeyVerified(string providerName, bool verified)
        {
            var entry = apiKeys.Find(k => k.provider == providerName);
            if (entry != null)
            {
                entry.isVerified = verified;
                Save();
            }
        }

        /// <summary>
        /// Validates API key format for a given provider
        /// </summary>
        public static bool ValidateApiKeyFormat(string providerName, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;

            switch (providerName)
            {
                case "Claude":
                    // Claude API keys start with "sk-ant-"
                    return apiKey.StartsWith("sk-ant-") && apiKey.Length > 20;

                case "OpenAI":
                    // OpenAI API keys start with "sk-" (but not "sk-ant-")
                    return apiKey.StartsWith("sk-") && !apiKey.StartsWith("sk-ant-") && apiKey.Length > 20;

                case "Gemini":
                    // Gemini API keys start with "AIza"
                    return apiKey.StartsWith("AIza") && apiKey.Length > 30;

                default:
                    // Generic validation: at least 20 characters
                    return apiKey.Length >= 20;
            }
        }
    }
}
