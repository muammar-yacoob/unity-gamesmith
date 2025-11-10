using System.IO;
using UnityEditor;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Manages custom AI rules stored in ProjectSettings.
    /// Users can override default system prompts with project-specific rules.
    /// </summary>
    public static class CustomAIRules
    {
        private const string RULES_FILE_PATH = "ProjectSettings/GameSmithAIRules.txt";
        private static string _cachedRules;
        private static bool _hasLoadedCache;

        /// <summary>
        /// Get custom AI rules if they exist, otherwise return empty string.
        /// Rules are appended to the base system prompt.
        /// </summary>
        public static string GetRules()
        {
            if (!_hasLoadedCache)
            {
                _cachedRules = LoadRules();
                _hasLoadedCache = true;
            }
            return _cachedRules ?? "";
        }

        /// <summary>
        /// Save custom AI rules to ProjectSettings.
        /// </summary>
        public static void SaveRules(string rules)
        {
            try
            {
                // Ensure ProjectSettings directory exists
                string directory = Path.GetDirectoryName(RULES_FILE_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save to file
                File.WriteAllText(RULES_FILE_PATH, rules);

                // Update cache
                _cachedRules = rules;
                _hasLoadedCache = true;

                Debug.Log("[GameSmith] Custom AI rules saved to ProjectSettings/GameSmithAIRules.txt");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSmith] Failed to save AI rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if custom rules file exists.
        /// </summary>
        public static bool HasCustomRules()
        {
            return File.Exists(RULES_FILE_PATH) && !string.IsNullOrWhiteSpace(GetRules());
        }

        /// <summary>
        /// Open custom rules file in external editor.
        /// Creates the file if it doesn't exist.
        /// </summary>
        public static void OpenInExternalEditor()
        {
            // Ensure file exists
            if (!File.Exists(RULES_FILE_PATH))
            {
                CreateDefaultRulesFile();
            }

            // Open in external editor
            EditorUtility.RevealInFinder(RULES_FILE_PATH);

            // Also open with default text editor
            System.Diagnostics.Process.Start(RULES_FILE_PATH);
        }

        /// <summary>
        /// Clear cache to force reload from file.
        /// </summary>
        public static void ClearCache()
        {
            _hasLoadedCache = false;
            _cachedRules = null;
        }

        private static string LoadRules()
        {
            try
            {
                if (File.Exists(RULES_FILE_PATH))
                {
                    return File.ReadAllText(RULES_FILE_PATH);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameSmith] Failed to load custom AI rules: {ex.Message}");
            }
            return "";
        }

        private static void CreateDefaultRulesFile()
        {
            string defaultContent = @"# Custom AI Rules for Game Smith

Add your project-specific rules here. These will be appended to the base system prompt.

## Example Rules:

- Always use PascalCase for C# class names
- Follow Unity naming conventions
- Prefer composition over inheritance
- Write XML documentation comments for public APIs
- Use async/await with UniTask instead of coroutines
- Follow SOLID principles

## Project-Specific Context:

Project Name: " + Application.productName + @"
Unity Version: " + Application.unityVersion + @"

## Custom Instructions:

(Add your instructions here)
";

            try
            {
                string directory = Path.GetDirectoryName(RULES_FILE_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(RULES_FILE_PATH, defaultContent);
                Debug.Log("[GameSmith] Created default AI rules file");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSmith] Failed to create default rules file: {ex.Message}");
            }
        }
    }
}
