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
    /// Lightweight JSON-backed chat history stored as a TextAsset
    /// </summary>
    public class ChatHistory
    {
        private const int MaxMessages = 200; // cap to prevent bloat
        private const string ResourcesRelativePath = "Assets/Resources/GameSmith";
        private const string JsonAssetPath = ResourcesRelativePath + "/ChatHistory.json";

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
#if UNITY_EDITOR
            EnsureFolders();
            File.WriteAllText(JsonAssetPath, json);
            UnityEditor.AssetDatabase.ImportAsset(JsonAssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
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

            // Try load TextAsset from Resources
            TextAsset ta = Resources.Load<TextAsset>("GameSmith/ChatHistory");
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<ChatHistoryWrapper>(ta.text);
                    if (loaded != null && loaded.messages != null)
                    {
                        history.messages = loaded.messages;
                        history.TrimToLimit();
                    }
                }
                catch { }
            }
            else
            {
#if UNITY_EDITOR
                EnsureFolders();
                if (!File.Exists(JsonAssetPath))
                {
                    File.WriteAllText(JsonAssetPath, JsonUtility.ToJson(new ChatHistoryWrapper { messages = new List<ChatMessage>() }, true));
                    UnityEditor.AssetDatabase.ImportAsset(JsonAssetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
#endif
            }

            return history;
        }

        [Serializable]
        private class ChatHistoryWrapper
        {
            public List<ChatMessage> messages;
        }

#if UNITY_EDITOR
        private static void EnsureFolders()
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!UnityEditor.AssetDatabase.IsValidFolder(ResourcesRelativePath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "GameSmith");
            }
        }
#endif
    }
}
