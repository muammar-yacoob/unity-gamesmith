using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Logging utility for GameSmith - logs to both Unity console and file
    /// </summary>
    public static class GameSmithLogger
    {
        private static string logFilePath;
        private static bool isEnabled = true;
        private static readonly object lockObject = new object();

        static GameSmithLogger()
        {
            // Create logs directory in project root
            string logsDir = Path.Combine(Application.dataPath, "..", "Logs", "GameSmith");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            // Create log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logsDir, $"gamesmith_{timestamp}.log");

            // Write header
            WriteToFile("=== GameSmith Session Started ===");
            WriteToFile($"Unity Version: {Application.unityVersion}");
            WriteToFile($"Platform: {Application.platform}");
            WriteToFile($"Time: {DateTime.Now}");
            WriteToFile("================================\n");
        }

        public static void Log(string message)
        {
            if (!isEnabled) return;

            string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            // Only write to file, not console (reduces spam)
            WriteToFile(formattedMessage);
        }

        public static void LogWarning(string message)
        {
            if (!isEnabled) return;

            string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] WARNING: {message}";
            Debug.LogWarning($"[GameSmith] {message}");
            WriteToFile(formattedMessage);
        }

        public static void LogError(string message, Exception ex = null)
        {
            if (!isEnabled) return;

            string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}";
            if (ex != null)
            {
                formattedMessage += $"\n  Exception: {ex.GetType().Name}\n  Message: {ex.Message}\n  StackTrace: {ex.StackTrace}";
            }

            Debug.LogError($"[GameSmith] {message}");
            if (ex != null)
            {
                Debug.LogException(ex);
            }
            WriteToFile(formattedMessage);
        }

        public static void LogRequest(string provider, string url, string requestBody)
        {
            if (!isEnabled) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] === API REQUEST ===");
            sb.AppendLine($"  Provider: {provider}");
            sb.AppendLine($"  URL: {url}");
            sb.AppendLine($"  Body Length: {requestBody?.Length ?? 0} bytes");

            // Only log first 500 chars of body to avoid huge logs
            if (!string.IsNullOrEmpty(requestBody))
            {
                string preview = requestBody.Length > 500 ? requestBody.Substring(0, 500) + "..." : requestBody;
                sb.AppendLine($"  Body Preview: {preview}");
            }
            sb.AppendLine("==================");

            WriteToFile(sb.ToString());
        }

        public static void LogResponse(string provider, bool success, string responseText, string error = null)
        {
            if (!isEnabled) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] === API RESPONSE ===");
            sb.AppendLine($"  Provider: {provider}");
            sb.AppendLine($"  Success: {success}");

            if (success && !string.IsNullOrEmpty(responseText))
            {
                sb.AppendLine($"  Response Length: {responseText.Length} bytes");
                string preview = responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText;
                sb.AppendLine($"  Response Preview: {preview}");
            }
            else if (!success && !string.IsNullOrEmpty(error))
            {
                sb.AppendLine($"  Error: {error}");
            }
            sb.AppendLine("===================");

            WriteToFile(sb.ToString());
        }

        public static string GetLogFilePath()
        {
            return logFilePath;
        }

        public static void OpenLogFile()
        {
            if (File.Exists(logFilePath))
            {
                System.Diagnostics.Process.Start(logFilePath);
            }
        }

        public static void OpenLogFolder()
        {
            string folder = Path.GetDirectoryName(logFilePath);
            if (Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start(folder);
            }
        }

        private static void WriteToFile(string message)
        {
            try
            {
                lock (lockObject)
                {
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // If we can't write to file, at least log to console
                Debug.LogError($"[GameSmith] Failed to write to log file: {ex.Message}");
            }
        }
    }
}