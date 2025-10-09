using UnityEditor;
using UnityEngine;
using System.Collections;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// [DEPRECATED] Legacy Unity Editor window - Use Tools/Game Smith instead
    /// </summary>
    [System.Obsolete("This window is deprecated. Please use Tools/Game Smith instead.")]
    public class UnityAIAgentWindow : EditorWindow
    {
        private AIAgentConfig config;
        private AIAgentClient client;
        private Vector2 scrollPosition;
        private string commandInput = "";
        private string responseOutput = "";
        private bool isProcessing = false;

        [MenuItem("Tools/Legacy/Unity AI Agent (Deprecated)", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityAIAgentWindow>("Unity AI Agent");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            config = AIAgentConfig.Load();
            client = new AIAgentClient(config);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawConfiguration();
            DrawQuickActions();
            DrawCommandInterface();
            DrawResponse();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Unity AI Agent", headerStyle);
            EditorGUILayout.LabelField("AI-Powered Game Development", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(10);
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("AI Configuration", EditorStyles.boldLabel);

            config.apiUrl = EditorGUILayout.TextField("API URL", config.apiUrl);
            config.model = EditorGUILayout.TextField("Model", config.model);
            config.apiKey = EditorGUILayout.PasswordField("API Key", config.apiKey);
            config.timeout = EditorGUILayout.IntField("Timeout (seconds)", config.timeout);
            config.temperature = EditorGUILayout.Slider("Temperature", config.temperature, 0f, 1f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Config"))
            {
                config.Save();
                EditorUtility.DisplayDialog("Success", "Configuration saved successfully!", "OK");
            }
            if (GUILayout.Button("Load Config"))
            {
                config = AIAgentConfig.Load();
                client = new AIAgentClient(config);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Create 2D Shooter Project", GUILayout.Height(30)))
            {
                ExecuteCommand("Create a complete 2D top-down shooter project with player movement, enemies, and shooting mechanics");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Player"))
            {
                ExecuteCommand("Create a player GameObject with movement controls, shooting, and health system");
            }
            if (GUILayout.Button("Create Enemy AI"))
            {
                ExecuteCommand("Create an enemy with AI that chases and attacks the player");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Projectile System"))
            {
                ExecuteCommand("Create a projectile system with physics and collision detection");
            }
            if (GUILayout.Button("Setup Level System"))
            {
                ExecuteCommand("Create a level management system with wave-based enemy spawning");
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Game UI"))
            {
                ExecuteCommand("Create game UI with health bar, score display, and menu screens");
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawCommandInterface()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Natural Language Commands", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Enter your command:", EditorStyles.miniLabel);
            commandInput = EditorGUILayout.TextArea(commandInput, GUILayout.Height(80));

            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(commandInput));
            if (GUILayout.Button(isProcessing ? "Processing..." : "Execute Command", GUILayout.Height(30)))
            {
                ExecuteCommand(commandInput);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawResponse()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("AI Response", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(responseOutput))
            {
                EditorGUILayout.TextArea(responseOutput, GUILayout.ExpandHeight(true));
            }
            else
            {
                EditorGUILayout.HelpBox("No response yet. Execute a command to see results here.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void ExecuteCommand(string command)
        {
            if (isProcessing) return;

            isProcessing = true;
            responseOutput = "Processing command...";
            Repaint();

            EditorCoroutineUtility.StartCoroutine(
                client.SendPromptAsync(
                    GeneratePrompt(command),
                    OnSuccess,
                    OnError
                ),
                this
            );
        }

        private string GeneratePrompt(string userCommand)
        {
            return $@"You are a Unity game development assistant. Generate C# code for the following request:

{userCommand}

Requirements:
- Generate complete, working Unity C# scripts
- Include all necessary using statements
- Add helpful comments
- Follow Unity best practices
- Make code production-ready

Provide only the C# code without explanations.";
        }

        private void OnSuccess(string response)
        {
            isProcessing = false;
            responseOutput = response;
            Repaint();
        }

        private void OnError(string error)
        {
            isProcessing = false;
            responseOutput = $"Error: {error}";
            EditorUtility.DisplayDialog("Error", error, "OK");
            Repaint();
        }
    }

    /// <summary>
    /// Utility for running coroutines in Editor mode
    /// </summary>
    public static class EditorCoroutineUtility
    {
        public static void StartCoroutine(IEnumerator routine, EditorWindow window)
        {
            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= update;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Editor coroutine error: {e.Message}");
                    EditorApplication.update -= update;
                }
            };
            EditorApplication.update += update;
        }
    }
}
