using System;
using System.IO;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Logging utility for AI Agent operations
    /// </summary>
    public static class AIAgentLogger
    {
        private static string LogPath => Path.Combine(Application.dataPath, "..", "Logs", "AIAgent.log");

        static AIAgentLogger()
        {
            string logDir = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        public static void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}";
            Debug.Log($"AI Agent: {message}");
            WriteToFile(logEntry);
        }

        public static void LogWarning(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] {message}";
            Debug.LogWarning($"AI Agent: {message}");
            WriteToFile(logEntry);
        }

        public static void LogError(string message, Exception exception = null)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}";
            if (exception != null)
            {
                logEntry += $"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}";
            }

            Debug.LogError($"AI Agent: {message}");
            WriteToFile(logEntry);
        }

        public static void LogSuccess(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SUCCESS] {message}";
            Debug.Log($"AI Agent SUCCESS: {message}");
            WriteToFile(logEntry);
        }

        private static void WriteToFile(string logEntry)
        {
            try
            {
                File.AppendAllText(LogPath, logEntry + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to write to log file: {e.Message}");
            }
        }

        public static void ClearLog()
        {
            try
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                    Debug.Log("AI Agent log cleared");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to clear log file: {e.Message}");
            }
        }
    }
}
