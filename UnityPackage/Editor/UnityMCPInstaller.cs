using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Installer for Unity MCP Editor Bridge Component
    /// Copies C# scripts from npm package templates to user's Unity project
    /// </summary>
    public static class UnityMCPInstaller
    {
        private const string INSTALL_PATH = "Assets/Editor/UnityMCP";
        private const string TEMPLATE_PATH_RELATIVE = "node_modules/@spark-apps/unity-mcp/dist/unity/templates";

        [MenuItem("Tools/GameSmith/Install Unity MCP Bridge", false, 50)]
        public static void ShowInstallDialog()
        {
            if (IsInstalled())
            {
                if (EditorUtility.DisplayDialog("Unity MCP Bridge",
                    "Unity MCP Bridge is already installed.\n\nReinstall?",
                    "Reinstall", "Cancel"))
                {
                    InstallBridge();
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("Unity MCP Bridge Required",
                    "Unity MCP Bridge is not installed.\n\n" +
                    "This component creates an HTTP server (port 8080) in Unity Editor that allows MCP tools to work.\n\n" +
                    "Without it, MCP tools will fail with: 'Unity Editor MCP server is not running'\n\n" +
                    "Install now?",
                    "Install", "Cancel"))
                {
                    InstallBridge();
                }
            }
        }

        public static bool IsInstalled()
        {
            return Directory.Exists(INSTALL_PATH) &&
                   File.Exists(Path.Combine(INSTALL_PATH, "MCPEditorServer.cs"));
        }

        public static void InstallBridge()
        {
            try
            {
                // Find package root (where node_modules is)
                string packageRoot = FindPackageRoot();
                if (string.IsNullOrEmpty(packageRoot))
                {
                    EditorUtility.DisplayDialog("Installation Failed",
                        "Could not find GameSmith package root.\n\n" +
                        "Expected to find: node_modules/@spark-apps/unity-mcp",
                        "OK");
                    return;
                }

                string templatesPath = Path.Combine(packageRoot, TEMPLATE_PATH_RELATIVE);
                if (!Directory.Exists(templatesPath))
                {
                    EditorUtility.DisplayDialog("Installation Failed",
                        $"Could not find MCP templates at:\n{templatesPath}\n\n" +
                        "Run 'npm install' in the package root.",
                        "OK");
                    return;
                }

                // Create installation directory
                Directory.CreateDirectory(INSTALL_PATH);

                // Copy and process template files
                string[] templates = {
                    "MCPConfig.cs.hbs",
                    "MCPEditorServer.cs.hbs",
                    "EditorCommandHandler.cs.hbs",
                    "PlayModeHandler.cs.hbs",
                    "SceneHandler.cs.hbs",
                    "AssetHandler.cs.hbs",
                    "AdvancedToolsHandler.cs.hbs"
                };

                int installed = 0;
                foreach (string template in templates)
                {
                    string templatePath = Path.Combine(templatesPath, template);
                    if (!File.Exists(templatePath))
                    {
                        Debug.LogWarning($"[Unity MCP] Template not found: {template}");
                        continue;
                    }

                    string scriptName = template.Replace(".hbs", "");

                    // Read template content
                    string content = File.ReadAllText(templatePath);

                    // Fix namespace (unity-mcp → UnityMCP)
                    content = content.Replace("namespace unity-mcp", "namespace UnityMCP");

                    // Fix Menu.MenuItemExists (doesn't exist in older Unity versions)
                    content = content.Replace("Menu.MenuItemExists", "MenuItemExistsCompat");
                    content = AddMenuItemExistsCompatIfNeeded(content);

                    // Note: Threading issues exist in upstream unity-mcp package
                    // Since DirectMCP is disabled, tools go through AI path only
                    // No point in trying to patch the threading here

                    // Write to installation path
                    string scriptPath = Path.Combine(INSTALL_PATH, scriptName);
                    File.WriteAllText(scriptPath, content);

                    // Create .meta file
                    CreateMetaFile(scriptPath);

                    installed++;
                }

                // Create README
                CreateReadme();

                // Refresh asset database
                AssetDatabase.Refresh();

                Debug.Log($"[Unity MCP] Installed {installed} scripts to {INSTALL_PATH}");
                Debug.LogWarning("[Unity MCP] KNOWN ISSUE: The Unity MCP bridge has threading issues. " +
                    "MCP tools may fail with 'GetActiveScene can only be called from the main thread'. " +
                    "The AI can still provide helpful guidance even if tools fail.");

                EditorUtility.DisplayDialog("Installation Complete (With Known Issue)",
                    $"Unity MCP Bridge installed to: {INSTALL_PATH}\n\n" +
                    $"✓ Scripts: {installed}\n" +
                    $"✓ HTTP server will auto-start on port 8080\n\n" +
                    "⚠️ KNOWN ISSUE:\n" +
                    "Unity MCP has threading bugs that prevent tools from executing.\n" +
                    "Tools will fail with threading errors.\n\n" +
                    "The AI will still receive error messages and can provide guidance.\n\n" +
                    "Report issue: github.com/muammar-yacoob/unity-mcp",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity MCP] Installation failed: {ex.Message}");
                EditorUtility.DisplayDialog("Installation Failed",
                    $"Error: {ex.Message}\n\nCheck Console for details.",
                    "OK");
            }
        }

        private static string FindPackageRoot()
        {
            // Start from this script's location and search upward
            string currentPath = Path.GetDirectoryName(Application.dataPath);

            for (int i = 0; i < 5; i++) // Search up to 5 levels
            {
                string nodeModulesPath = Path.Combine(currentPath, "node_modules/@spark-apps/unity-mcp");
                if (Directory.Exists(nodeModulesPath))
                {
                    return currentPath;
                }

                currentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(currentPath))
                    break;
            }

            return null;
        }

        private static string AddMenuItemExistsCompatIfNeeded(string content)
        {
            // If content uses MenuItemExistsCompat, add the helper method
            if (content.Contains("MenuItemExistsCompat"))
            {
                // Find the class definition and add the helper method
                string helperMethod = @"
        private static bool MenuItemExistsCompat(string menuPath)
        {
            // Compatibility shim for older Unity versions
            try
            {
                var menuItemMethod = typeof(UnityEditor.Menu).GetMethod(""MenuItemExists"",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (menuItemMethod != null)
                {
                    return (bool)menuItemMethod.Invoke(null, new object[] { menuPath });
                }
            }
            catch { }
            return false; // Assume doesn't exist in older Unity
        }
";
                // Add after the first class opening brace
                int classPos = content.IndexOf("public static class ");
                if (classPos > 0)
                {
                    int bracePos = content.IndexOf("{", classPos);
                    if (bracePos > 0)
                    {
                        content = content.Insert(bracePos + 1, helperMethod);
                    }
                }
            }
            return content;
        }

        private static void CreateMetaFile(string scriptPath)
        {
            string guid = Guid.NewGuid().ToString("N");
            string metaContent = $@"fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
";
            File.WriteAllText(scriptPath + ".meta", metaContent);
        }

        private static void CreateReadme()
        {
            string readme = @"# Unity MCP Editor Bridge

**DO NOT DELETE THIS FOLDER** - Required for MCP tools to work!

## What This Does

Creates an HTTP server (port 8080) inside Unity Editor that allows the Node.js MCP server to execute Unity operations.

## Architecture

```
GameSmith Chat → Node.js MCP (33 tools) → HTTP :8080 → Unity C# Bridge → Unity API
```

## Scripts Installed

1. MCPConfig.cs - Configuration (ScriptableObject)
2. MCPEditorServer.cs - HTTP server (auto-starts on port 8080)
3. EditorCommandHandler.cs - Select, transform, align objects
4. SceneHandler.cs - Get hierarchy, find objects
5. PlayModeHandler.cs - Play mode automation
6. AssetHandler.cs - Console, assets, prefabs
7. AdvancedToolsHandler.cs - Advanced operations

## Verification

After Unity restarts, check Console for:
```
[Unity MCP] Server started on port 8080
```

## Reinstalling

Menu: `Tools → GameSmith → Install Unity MCP Bridge`

## Documentation

https://github.com/muammar-yacoob/unity-mcp
";
            string readmePath = Path.Combine(INSTALL_PATH, "README.md");
            File.WriteAllText(readmePath, readme);
            CreateMetaFile(readmePath);
        }
    }
}
