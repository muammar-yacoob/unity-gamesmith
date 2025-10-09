using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Provides context about the current Unity project
    /// </summary>
    public static class UnityProjectContext
    {
        public static string GetProjectContext()
        {
            var context = new StringBuilder();

            context.AppendLine("=== Unity Project Context ===\n");

            // Project info
            context.AppendLine($"Project: {Application.productName}");
            context.AppendLine($"Unity Version: {Application.unityVersion}");
            context.AppendLine($"Platform: {EditorUserBuildSettings.activeBuildTarget}\n");

            // Current scene
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            context.AppendLine($"Current Scene: {currentScene.name}");
            context.AppendLine($"Scene Path: {currentScene.path}\n");

            // Project structure
            context.AppendLine("Project Structure:");
            var scriptsFolder = "Assets/Scripts";
            if (Directory.Exists(scriptsFolder))
            {
                var scriptFiles = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories)
                    .Take(20) // Limit to 20 files
                    .Select(f => f.Replace("\\", "/").Replace(Application.dataPath, "Assets"));

                context.AppendLine("  Scripts:");
                foreach (var script in scriptFiles)
                {
                    context.AppendLine($"    - {script}");
                }
            }

            // Currently selected GameObject
            if (Selection.activeGameObject != null)
            {
                context.AppendLine($"\nCurrently Selected: {Selection.activeGameObject.name}");

                var components = Selection.activeGameObject.GetComponents<Component>();
                if (components.Length > 0)
                {
                    context.AppendLine("  Components:");
                    foreach (var comp in components)
                    {
                        if (comp != null)
                        {
                            context.AppendLine($"    - {comp.GetType().Name}");
                        }
                    }
                }
            }

            // Installed packages
            context.AppendLine("\nKey Installed Packages:");
            var packagesPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (File.Exists(packagesPath))
            {
                var manifestContent = File.ReadAllText(packagesPath);
                // Parse basic package info (simple approach)
                var lines = manifestContent.Split('\n')
                    .Where(l => l.Contains("com.unity") || l.Contains("com."))
                    .Take(10);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim().TrimEnd(',');
                    if (!string.IsNullOrEmpty(trimmed) && trimmed.Contains(":"))
                    {
                        context.AppendLine($"  {trimmed}");
                    }
                }
            }

            return context.ToString();
        }

        public static string GetSelectedObjectContext()
        {
            if (Selection.activeGameObject == null)
            {
                return "No GameObject selected.";
            }

            var context = new StringBuilder();
            var go = Selection.activeGameObject;

            context.AppendLine($"Selected GameObject: {go.name}");
            context.AppendLine($"Tag: {go.tag}");
            context.AppendLine($"Layer: {LayerMask.LayerToName(go.layer)}");
            context.AppendLine($"Active: {go.activeInHierarchy}");

            context.AppendLine("\nComponents:");
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp != null)
                {
                    context.AppendLine($"  - {comp.GetType().FullName}");
                }
            }

            return context.ToString();
        }
    }
}
