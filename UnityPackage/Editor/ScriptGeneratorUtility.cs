using System.IO;
using UnityEditor;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Utility for generating and saving Unity C# scripts
    /// </summary>
    public static class ScriptGeneratorUtility
    {
        public static void CreateScript(string scriptName, string content, string relativePath = "Scripts")
        {
            string folderPath = Path.Combine(Application.dataPath, relativePath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"{scriptName}.cs");

            File.WriteAllText(filePath, content);

            AssetDatabase.Refresh();

            Debug.Log($"Script created: {filePath}");
        }

        public static void CreateFolder(string relativePath)
        {
            string folderPath = Path.Combine(Application.dataPath, relativePath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }
        }

        public static string GetAssetPath(string scriptName, string relativePath = "Scripts")
        {
            return $"Assets/{relativePath}/{scriptName}.cs";
        }
    }
}
