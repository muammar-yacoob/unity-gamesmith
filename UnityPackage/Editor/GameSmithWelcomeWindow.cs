using UnityEditor;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Welcome window shown on first-time setup
    /// Provides friendly, zero-configuration onboarding
    /// </summary>
    public class GameSmithWelcomeWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        // removed unused setupComplete flag
        private bool dontShowAgain = false;
        private const string HIDE_WELCOME_KEY = "GameSmith_HideWelcome";

        public static void ShowWindow()
        {
            var window = GetWindow<GameSmithWelcomeWindow>(true, "GameSmith", true);
            var size = new Vector2(480, 420);
            window.minSize = size;
            window.maxSize = size;
            // Center in main editor window
            Rect main = GetMainWindowPosition();
            var x = main.x + (main.width - size.x) / 2f;
            var y = main.y + (main.height - size.y) / 2f;
            window.position = new Rect(x, y, size.x, size.y);
            window.ShowUtility();
        }

        private static Rect GetMainWindowPosition()
        {
            var containerWinType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ContainerWindow");
            var showModeField = containerWinType?.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionProperty = containerWinType?.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (containerWinType == null || showModeField == null || positionProperty == null)
                return new Rect(200, 200, 800, 600);

            var windows = Resources.FindObjectsOfTypeAll(containerWinType);
            foreach (var win in windows)
            {
                var showModeValue = showModeField.GetValue(win);
                if (showModeValue != null)
                {
                    int showmode = (int)showModeValue;
                    // 4 == main window
                    if (showmode == 4)
                    {
                        return (Rect)positionProperty.GetValue(win, null);
                    }
                }
            }
            return new Rect(200, 200, 800, 600);
        }

        private void OnEnable()
        {
            dontShowAgain = EditorPrefs.GetBool(HIDE_WELCOME_KEY, false);
        }

        private void OnGUI()
        {
            // Header
            GUILayout.Space(20);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("⚒️ GameSmith", titleStyle);

            GUILayout.Space(5);

            // Subtitle
            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            EditorGUILayout.LabelField("AI-Powered Unity Development Assistant", subtitleStyle);

            GUILayout.Space(15);

            // About section
            DrawInfoBox("What is GameSmith?",
                "GameSmith brings AI assistance directly into Unity. Chat with Claude, Gemini, OpenAI, or run models locally with Ollama.\n\n" +
                "• Privacy-first: Your code stays local\n" +
                "• Zero telemetry & open source\n" +
                "• Works with your project context");

            GUILayout.Space(10);

            // Status box
            DrawStatusBox();

            GUILayout.Space(15);

            // Primary action button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            bool hasApiKey = false;
            var settings = GameSmithSettings.Instance;
            if (settings != null && settings.apiKeys != null)
            {
                foreach (var key in settings.apiKeys)
                {
                    if (!string.IsNullOrEmpty(key.apiKey))
                    {
                        hasApiKey = true;
                        break;
                    }
                }
            }

            if (hasApiKey)
            {
                if (GUILayout.Button("Open GameSmith", buttonStyle, GUILayout.Height(35), GUILayout.Width(180)))
                {
                    GameSmithWindow.ShowWindow();
                    Close();
                }
            }
            else
            {
                if (GUILayout.Button("Configure Settings", buttonStyle, GUILayout.Height(35), GUILayout.Width(180)))
                {
                    OpenSettings();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Help resources
            DrawHelpLinks();

            GUILayout.Space(12);

            // Don't show again checkbox
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var checkboxStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 10
            };

            bool newVal = GUILayout.Toggle(dontShowAgain, "Don't show this again", checkboxStyle);
            if (newVal != dontShowAgain)
            {
                dontShowAgain = newVal;
                EditorPrefs.SetBool(HIDE_WELCOME_KEY, dontShowAgain);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void DrawInfoBox(string title, string content)
        {
            var boxStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(12, 12, 10, 10)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            EditorGUILayout.LabelField(title, headerStyle);

            GUILayout.Space(5);

            var contentStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11,
                richText = true
            };
            EditorGUILayout.LabelField(content, contentStyle);

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBox()
        {
            var settings = GameSmithSettings.Instance;
            bool hasApiKey = false;

            if (settings != null && settings.apiKeys != null)
            {
                foreach (var key in settings.apiKeys)
                {
                    if (!string.IsNullOrEmpty(key.apiKey))
                    {
                        hasApiKey = true;
                        break;
                    }
                }
            }

            var statusBoxStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(10, 10, 8, 8),
                alignment = TextAnchor.MiddleCenter
            };

            if (!hasApiKey)
            {
                statusBoxStyle.normal.background = MakeTex(2, 2, new Color(0.4f, 0.3f, 0.1f, 0.3f));
            }

            EditorGUILayout.BeginVertical(statusBoxStyle);

            var statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            if (hasApiKey)
            {
                statusStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
                EditorGUILayout.LabelField("✓ Ready to use", statusStyle);
            }
            else
            {
                statusStyle.normal.textColor = new Color(0.9f, 0.7f, 0.3f);
                EditorGUILayout.LabelField("⚠ Setup Required", statusStyle);
                GUILayout.Space(3);

                var hintStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
                EditorGUILayout.LabelField("Select an AI provider and add your API key to get started", hintStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHelpLinks()
        {
            var linkBoxStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(12, 12, 8, 8)
            };

            EditorGUILayout.BeginVertical(linkBoxStyle);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField("Need Help?", headerStyle);

            GUILayout.Space(5);

            var linkStyle = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = 10
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            if (GUILayout.Button("📖 Documentation", linkStyle))
            {
                Application.OpenURL("https://github.com/muammar-yacoob/unity-gamesmith");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.Space(15);

            if (GUILayout.Button("💬 Discord Server", linkStyle))
            {
                Application.OpenURL("https://discord.gg/your-discord-invite");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.Space(15);

            if (GUILayout.Button("🐛 Report Issues", linkStyle))
            {
                Application.OpenURL("https://github.com/muammar-yacoob/unity-gamesmith/issues");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OpenSettings()
        {
            GameSmithSettingsWindow.ShowWindow();
            Close();
        }
    }
}
