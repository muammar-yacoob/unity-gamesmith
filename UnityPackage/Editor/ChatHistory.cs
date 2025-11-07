using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Represents a single message in the chat
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public enum Role
        {
            User,
            Assistant,
            System
        }

        public Role role;
        public string content;
        public string timestamp;

        public ChatMessage(Role role, string content)
        {
            this.role = role;
            this.content = content;
            this.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// Lightweight JSON-backed chat history stored in Logs directory
    /// </summary>
    public class ChatHistory
    {
        private const int MaxMessages = 200; // cap to prevent bloat
        private static string LogsPath => System.IO.Path.Combine("Logs", "GameSmith");
        private static string JsonFilePath => System.IO.Path.Combine(LogsPath, "ChatHistory.json");

        [SerializeField]
        private List<ChatMessage> messages = new List<ChatMessage>();

        public IReadOnlyList<ChatMessage> Messages => messages;

        public void AddMessage(ChatMessage.Role role, string content)
        {
            messages.Add(new ChatMessage(role, content));
            TrimToLimit();
            SaveHistory();
        }

        public void ClearHistory()
        {
            messages.Clear();
            SaveHistory();
        }

        private void SaveHistory()
        {
            var wrapper = new ChatHistoryWrapper { messages = messages };
            var json = JsonUtility.ToJson(wrapper, prettyPrint: true);

            // Ensure Logs/GameSmith directory exists
            if (!Directory.Exists(LogsPath))
            {
                Directory.CreateDirectory(LogsPath);
            }

            File.WriteAllText(JsonFilePath, json);
        }

        private void TrimToLimit()
        {
            if (messages.Count <= MaxMessages) return;
            int removeCount = messages.Count - MaxMessages;
            messages.RemoveRange(0, removeCount);
        }

        /// <summary>
        /// Get or create the default chat history instance
        /// </summary>
        public static ChatHistory GetOrCreate()
        {
            var history = new ChatHistory();

            // Ensure Logs/GameSmith directory exists
            if (!Directory.Exists(LogsPath))
            {
                Directory.CreateDirectory(LogsPath);
            }

            if (File.Exists(JsonFilePath))
            {
                try
                {
                    string json = File.ReadAllText(JsonFilePath);
                    var loaded = JsonUtility.FromJson<ChatHistoryWrapper>(json);
                    if (loaded != null && loaded.messages != null)
                    {
                        history.messages = loaded.messages;
                        history.TrimToLimit();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameSmith] Failed to load chat history: {ex.Message}");
                }
            }
            else
            {
                // Create empty history file
                File.WriteAllText(JsonFilePath, JsonUtility.ToJson(new ChatHistoryWrapper { messages = new List<ChatMessage>() }, true));
            }

            return history;
        }

        [Serializable]
        private class ChatHistoryWrapper
        {
            public List<ChatMessage> messages;
        }
    }
}
