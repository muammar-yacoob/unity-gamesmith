using System;
using System.Collections.Generic;
using UnityEngine;

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
    /// ScriptableObject to store chat history
    /// </summary>
    [CreateAssetMenu(fileName = "ChatHistory", menuName = "Game Smith/Chat History")]
    public class ChatHistory : ScriptableObject
    {
        [SerializeField]
        private List<ChatMessage> messages = new List<ChatMessage>();

        public IReadOnlyList<ChatMessage> Messages => messages;

        public void AddMessage(ChatMessage.Role role, string content)
        {
            messages.Add(new ChatMessage(role, content));
            SaveHistory();
        }

        public void ClearHistory()
        {
            messages.Clear();
            SaveHistory();
        }

        private void SaveHistory()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Get or create the default chat history instance
        /// </summary>
        public static ChatHistory GetOrCreate()
        {
            var history = Resources.Load<ChatHistory>("ChatHistory");
            if (history == null)
            {
                history = CreateInstance<ChatHistory>();
#if UNITY_EDITOR
                var resourcesPath = "Assets/Resources";
                if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                }
                UnityEditor.AssetDatabase.CreateAsset(history, $"{resourcesPath}/ChatHistory.asset");
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }
            return history;
        }
    }
}
