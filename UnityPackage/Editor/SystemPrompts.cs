using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Loads and provides system prompts from Resources folder.
    /// Keeps GameSmithWindow.cs clean by externalizing large prompt strings.
    /// </summary>
    public static class SystemPrompts
    {
        private static string _withToolsPrompt;
        private static string _withoutToolsPrompt;

        /// <summary>
        /// System prompt for when MCP tools are available.
        /// Includes instructions for tool usage and multi-step operations.
        /// Appends custom AI rules if they exist.
        /// </summary>
        public static string WithTools
        {
            get
            {
                if (_withToolsPrompt == null)
                {
                    var asset = Resources.Load<TextAsset>("GameSmith/Prompts/SystemPromptWithTools");
                    if (asset != null)
                    {
                        _withToolsPrompt = asset.text;
                    }
                    else
                    {
                        Debug.LogWarning("[GameSmith] SystemPromptWithTools.txt not found in Resources. Using fallback.");
                        _withToolsPrompt = GetFallbackWithToolsPrompt();
                    }
                }

                // Append custom rules if they exist
                string customRules = CustomAIRules.GetRules();
                if (!string.IsNullOrWhiteSpace(customRules))
                {
                    return _withToolsPrompt + "\n\n## Project-Specific Rules:\n" + customRules;
                }

                return _withToolsPrompt;
            }
        }

        /// <summary>
        /// System prompt for when no MCP tools are available.
        /// Simple instructions for general Unity development assistance.
        /// Appends custom AI rules if they exist.
        /// </summary>
        public static string WithoutTools
        {
            get
            {
                if (_withoutToolsPrompt == null)
                {
                    var asset = Resources.Load<TextAsset>("GameSmith/Prompts/SystemPromptWithoutTools");
                    if (asset != null)
                    {
                        _withoutToolsPrompt = asset.text;
                    }
                    else
                    {
                        Debug.LogWarning("[GameSmith] SystemPromptWithoutTools.txt not found in Resources. Using fallback.");
                        _withoutToolsPrompt = GetFallbackWithoutToolsPrompt();
                    }
                }

                // Append custom rules if they exist
                string customRules = CustomAIRules.GetRules();
                if (!string.IsNullOrWhiteSpace(customRules))
                {
                    return _withoutToolsPrompt + "\n\n## Project-Specific Rules:\n" + customRules;
                }

                return _withoutToolsPrompt;
            }
        }

        /// <summary>
        /// Clear cached prompts to force reload from Resources.
        /// Useful for testing prompt changes without restarting Unity.
        /// </summary>
        public static void ClearCache()
        {
            _withToolsPrompt = null;
            _withoutToolsPrompt = null;
        }

        private static string GetFallbackWithToolsPrompt()
        {
            return @"You are a Unity AI assistant with real-time access to the Unity Editor via MCP tools.

CRITICAL RULES:
1. The Unity MCP server IS RUNNING and tools ARE AVAILABLE - never say otherwise
2. When asked about scene objects, hierarchy, or Unity data: USE THE TOOLS IMMEDIATELY
3. Do not provide manual code or explanations - execute the appropriate tool
4. Tools have been verified and are ready to use
5. **ALWAYS provide REQUIRED parameters** - Never call tools with empty arguments {}!

⚠️ IMPORTANT: When operations require multiple steps, you MUST call ALL tools in your SINGLE response!
Do NOT call just the first tool and wait - call ALL tools together in one response.

COMMON OPERATIONS:
- MOVE object: transform_objects({""name"": ""Cube"", ""position"": [5, 0, 0]})
- ROTATE object: transform_objects({""name"": ""Cube"", ""rotation"": [0, 45, 0]})
- SCALE object: transform_objects({""name"": ""Cube"", ""scale"": [2, 2, 2]})
- DELETE object: select_objects({""name"": ""Cube""}) THEN delete_objects({})

⚠️ COMMON MISTAKES TO AVOID:
❌ WRONG: transform_objects({}) - Missing required ""name"" parameter!
✅ RIGHT: transform_objects({""name"": ""Cube"", ""position"": [5, 0, 0]})

❌ WRONG: select_objects({}) - Missing required ""name"" parameter!
✅ RIGHT: select_objects({""name"": ""Cube""})

❌ WRONG: execute_menu_item({}) - Missing required ""menuPath"" parameter!
✅ RIGHT: execute_menu_item({""menuPath"": ""GameObject/3D Object/Cube""})

Examples:
- ""delete the sphere"" → select_objects({""name"": ""Sphere""}) + delete_objects({})
- ""list objects"" → get_hierarchy({})
- ""move cube to 5,0,0"" → transform_objects({""name"": ""Cube"", ""position"": [5, 0, 0]})";
        }

        private static string GetFallbackWithoutToolsPrompt()
        {
            return @"You are a Unity AI assistant helping with game development.

Focus on providing clear, actionable advice for Unity development. When asked to write code, provide complete, working examples.";
        }
    }
}
