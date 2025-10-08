using System;
using System.IO;
using UnityEngine;

namespace SparkGames.UnityAIAgent.Editor
{
    /// <summary>
    /// Configuration for AI agent API integration
    /// </summary>
    [Serializable]
    public class AIAgentConfig
    {
        public string apiUrl = "http://localhost:11434/api/generate";
        public string model = "codellama";
        public string apiKey = "";
        public int timeout = 120;
        public float temperature = 0.7f;

        private static string ConfigPath => Path.Combine(Application.dataPath, "..", "AIAgentConfig.json");

        public static AIAgentConfig Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonUtility.FromJson<AIAgentConfig>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load AI Agent config: {e.Message}");
                }
            }

            // Return default config
            return new AIAgentConfig();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(ConfigPath, json);
                Debug.Log($"AI Agent config saved to: {ConfigPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save AI Agent config: {e.Message}");
            }
        }
    }
}
