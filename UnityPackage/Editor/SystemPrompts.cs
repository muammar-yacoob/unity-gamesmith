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

⚠️ IMPORTANT: When operations require multiple steps, you MUST call ALL tools in your SINGLE response!
Do NOT call just the first tool and wait - call BOTH tools together in one response.

COMMON OPERATIONS (call ALL tools in ONE response):
- DELETE object: Call BOTH unity_select_objects(pattern) AND unity_delete_objects() in same response
- MOVE object: Call BOTH unity_select_objects(pattern) AND unity_transform_objects(position) in same response
- ROTATE object: Call BOTH unity_select_objects(pattern) AND unity_transform_objects(rotation) in same response
- SCALE object: Call BOTH unity_select_objects(pattern) AND unity_transform_objects(scale) in same response

WRONG: Using unity_find_in_scene for operations - it only LISTS objects, doesn't select them!
WRONG: Calling only unity_select_objects and stopping - you must also call the action tool!
RIGHT: Call unity_select_objects AND the action tool TOGETHER in the same response

Examples:
- ""delete the sphere"" → Use TWO tool_use blocks: unity_select_objects(pattern=""Sphere"") + unity_delete_objects()
- ""list objects"" → Use ONE tool: unity_get_hierarchy()
- ""move cube to 5,0,0"" → Use TWO tool_use blocks: unity_select_objects(pattern=""Cube"") + unity_transform_objects(position=[5,0,0])";
        }

        private static string GetFallbackWithoutToolsPrompt()
        {
            return @"You are a Unity AI assistant helping with game development.

Focus on providing clear, actionable advice for Unity development. When asked to write code, provide complete, working examples.";
        }
    }
}
