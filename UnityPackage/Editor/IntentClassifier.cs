using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Classifies user intent to route Unity operations directly to MCP tools
    /// without AI overhead, while preserving AI for complex reasoning tasks
    /// </summary>
    public static class IntentClassifier
    {
        /// <summary>
        /// Classification result indicating how to handle user input
        /// </summary>
        public class IntentResult
        {
            public IntentType Type { get; set; }
            public string ToolName { get; set; }
            public Dictionary<string, object> Arguments { get; set; }
            public string UserMessage { get; set; }

            public IntentResult()
            {
                Arguments = new Dictionary<string, object>();
            }
        }

        public enum IntentType
        {
            DirectMCP,      // Execute MCP tool directly
            RequiresAI,     // Needs AI reasoning/generation
            AmbiguousWithTools  // Might need tools, send to AI with tools
        }

        private static readonly Dictionary<Regex, Func<Match, IntentResult>> DirectPatterns = new Dictionary<Regex, Func<Match, IntentResult>>
        {
            // Scene Hierarchy
            { new Regex(@"^(list|show|get)\s+(objects?|hierarchy|scene)", RegexOptions.IgnoreCase),
                m => CreateResult("unity_get_hierarchy", new Dictionary<string, object>()) },

            { new Regex(@"^(what|show)\s+(is|are)\s+in\s+(the\s+)?scene", RegexOptions.IgnoreCase),
                m => CreateResult("unity_get_hierarchy", new Dictionary<string, object>()) },

            // Select Objects
            { new Regex(@"^select\s+(?:object\s+)?[""']?(\w+)[""']?", RegexOptions.IgnoreCase),
                m => CreateResult("select_gameobject", new Dictionary<string, object> { { "name", m.Groups[1].Value } }) },

            { new Regex(@"^find\s+(?:and\s+select\s+)?[""']?(\w+)[""']?", RegexOptions.IgnoreCase),
                m => CreateResult("select_gameobject", new Dictionary<string, object> { { "name", m.Groups[1].Value } }) },

            // Create Objects (primitives)
            { new Regex(@"^create\s+(?:a\s+)?(cube|sphere|cylinder|plane|capsule|quad)", RegexOptions.IgnoreCase),
                m => CreateResult("create_object", new Dictionary<string, object> { { "type", CapitalizeFirst(m.Groups[1].Value) } }) },

            // Transform Operations - Move
            { new Regex(@"^move\s+(?:the\s+)?(\w+)\s+(?:to\s+)?(?:position\s+)?\(?(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\)?", RegexOptions.IgnoreCase),
                m => CreateResult("translate_object", new Dictionary<string, object>
                {
                    { "name", m.Groups[1].Value },
                    { "x", float.Parse(m.Groups[2].Value) },
                    { "y", float.Parse(m.Groups[3].Value) },
                    { "z", float.Parse(m.Groups[4].Value) }
                }) },

            // Transform Operations - Scale
            { new Regex(@"^scale\s+(?:the\s+)?(\w+)\s+(?:to\s+)?(?:by\s+)?(\d+\.?\d*)(?:x)?", RegexOptions.IgnoreCase),
                m => {
                    var scale = float.Parse(m.Groups[2].Value);
                    return CreateResult("scale_object", new Dictionary<string, object>
                    {
                        { "name", m.Groups[1].Value },
                        { "x", scale },
                        { "y", scale },
                        { "z", scale }
                    });
                }},

            // Transform Operations - Rotate
            { new Regex(@"^rotate\s+(?:the\s+)?(\w+)\s+(?:to\s+)?(?:by\s+)?(-?\d+\.?\d*)\s*(?:degrees?)?", RegexOptions.IgnoreCase),
                m => CreateResult("rotate_object", new Dictionary<string, object>
                {
                    { "name", m.Groups[1].Value },
                    { "angle", float.Parse(m.Groups[2].Value) }
                }) },

            // Delete Objects
            { new Regex(@"^(?:delete|remove|destroy)\s+(?:the\s+)?(\w+)", RegexOptions.IgnoreCase),
                m => CreateResult("delete_object", new Dictionary<string, object> { { "name", m.Groups[1].Value } }) },

            // Console Logs
            { new Regex(@"^(?:show|get|display)\s+(?:console\s+)?logs?", RegexOptions.IgnoreCase),
                m => CreateResult("get_console_logs", new Dictionary<string, object>()) },

            { new Regex(@"^clear\s+console", RegexOptions.IgnoreCase),
                m => CreateResult("clear_console", new Dictionary<string, object>()) },

            // Scene Operations
            { new Regex(@"^(?:list|show)\s+scenes?", RegexOptions.IgnoreCase),
                m => CreateResult("list_scenes", new Dictionary<string, object>()) },

            { new Regex(@"^(?:load|open)\s+scene\s+[""']?(\w+)[""']?", RegexOptions.IgnoreCase),
                m => CreateResult("load_scene", new Dictionary<string, object> { { "name", m.Groups[1].Value } }) },

            { new Regex(@"^save\s+scene", RegexOptions.IgnoreCase),
                m => CreateResult("save_scene", new Dictionary<string, object>()) },

            // Play Mode
            { new Regex(@"^(?:enter|start)\s+play\s*mode", RegexOptions.IgnoreCase),
                m => CreateResult("enter_play_mode", new Dictionary<string, object>()) },

            { new Regex(@"^(?:exit|stop)\s+play\s*mode", RegexOptions.IgnoreCase),
                m => CreateResult("exit_play_mode", new Dictionary<string, object>()) },

            { new Regex(@"^(?:play|run)\s+(?:the\s+)?(?:game|scene)", RegexOptions.IgnoreCase),
                m => CreateResult("enter_play_mode", new Dictionary<string, object>()) },

            // Assets
            { new Regex(@"^(?:list|show|get)\s+assets?", RegexOptions.IgnoreCase),
                m => CreateResult("get_assets", new Dictionary<string, object>()) },

            { new Regex(@"^refresh\s+assets?", RegexOptions.IgnoreCase),
                m => CreateResult("refresh_assets", new Dictionary<string, object>()) },

            // Cleanup
            { new Regex(@"^clean\s*up\s+scene", RegexOptions.IgnoreCase),
                m => CreateResult("cleanup_scene", new Dictionary<string, object>()) },
        };

        // Patterns that indicate AI reasoning is needed
        private static readonly List<Regex> AIRequiredPatterns = new List<Regex>
        {
            new Regex(@"\b(write|create|generate|implement|refactor|optimize)\s+(script|code|class|function|method)", RegexOptions.IgnoreCase),
            new Regex(@"\b(how|why|what|when|explain|describe|document)", RegexOptions.IgnoreCase),
            new Regex(@"\b(fix|debug|solve|resolve)\s+(?:bug|error|issue|problem)", RegexOptions.IgnoreCase),
            new Regex(@"\b(design|architecture|pattern|best\s+practice)", RegexOptions.IgnoreCase),
            new Regex(@"\b(compare|analyze|review|suggest|recommend)", RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// Classify user intent and determine execution strategy
        /// </summary>
        public static IntentResult Classify(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return new IntentResult { Type = IntentType.RequiresAI, UserMessage = userInput };
            }

            var trimmedInput = userInput.Trim();

            // 1. Check for direct MCP operation patterns
            foreach (var pattern in DirectPatterns)
            {
                var match = pattern.Key.Match(trimmedInput);
                if (match.Success)
                {
                    var result = pattern.Value(match);
                    result.Type = IntentType.DirectMCP;
                    result.UserMessage = userInput;
                    return result;
                }
            }

            // 2. Check if AI reasoning is explicitly required
            foreach (var aiPattern in AIRequiredPatterns)
            {
                if (aiPattern.IsMatch(trimmedInput))
                {
                    return new IntentResult
                    {
                        Type = IntentType.RequiresAI,
                        UserMessage = userInput
                    };
                }
            }

            // 3. Ambiguous cases - might need tools, let AI decide
            // Examples: "make the player jump", "add physics to the cube"
            if (ContainsUnityTerms(trimmedInput))
            {
                return new IntentResult
                {
                    Type = IntentType.AmbiguousWithTools,
                    UserMessage = userInput
                };
            }

            // 4. Default to AI for general queries
            return new IntentResult
            {
                Type = IntentType.RequiresAI,
                UserMessage = userInput
            };
        }

        private static IntentResult CreateResult(string toolName, Dictionary<string, object> args)
        {
            return new IntentResult
            {
                ToolName = toolName,
                Arguments = args
            };
        }

        private static bool ContainsUnityTerms(string input)
        {
            string[] unityTerms = new[]
            {
                "gameobject", "game object", "transform", "component", "scene", "hierarchy",
                "prefab", "asset", "material", "mesh", "sprite", "animator", "rigidbody",
                "collider", "camera", "light", "particle", "audio", "script", "canvas",
                "ui", "button", "text", "image", "panel", "physics", "raycast"
            };

            var lowerInput = input.ToLower();
            return unityTerms.Any(term => lowerInput.Contains(term));
        }

        private static string CapitalizeFirst(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }

        /// <summary>
        /// Get user-friendly description of what will be executed
        /// </summary>
        public static string GetExecutionDescription(IntentResult intent)
        {
            if (intent.Type != IntentType.DirectMCP)
                return null;

            switch (intent.ToolName)
            {
                case "unity_get_hierarchy":
                    return "Listing scene objects...";
                case "select_gameobject":
                    return $"Selecting '{intent.Arguments.GetValueOrDefault("name")}'...";
                case "create_object":
                    return $"Creating {intent.Arguments.GetValueOrDefault("type")}...";
                case "translate_object":
                    return $"Moving '{intent.Arguments.GetValueOrDefault("name")}'...";
                case "scale_object":
                    return $"Scaling '{intent.Arguments.GetValueOrDefault("name")}'...";
                case "rotate_object":
                    return $"Rotating '{intent.Arguments.GetValueOrDefault("name")}'...";
                case "delete_object":
                    return $"Deleting '{intent.Arguments.GetValueOrDefault("name")}'...";
                case "get_console_logs":
                    return "Fetching console logs...";
                case "clear_console":
                    return "Clearing console...";
                case "list_scenes":
                    return "Listing scenes...";
                case "load_scene":
                    return $"Loading scene '{intent.Arguments.GetValueOrDefault("name")}'...";
                case "save_scene":
                    return "Saving scene...";
                case "enter_play_mode":
                    return "Entering play mode...";
                case "exit_play_mode":
                    return "Exiting play mode...";
                case "get_assets":
                    return "Listing assets...";
                case "refresh_assets":
                    return "Refreshing asset database...";
                case "cleanup_scene":
                    return "Cleaning up scene...";
                default:
                    return $"Executing {intent.ToolName}...";
            }
        }
    }
}
