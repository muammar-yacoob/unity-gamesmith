using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Unified Game Smith Editor Window - Modern, clean UI for AI-powered game development
    /// </summary>
    public class GameSmithWindow : EditorWindow
    {
        #region Fields
        private AIAgentConfig config;
        private AIAgentClient client;
        private Vector2 scrollPos;

        // AI Provider System
        private AIProviderDatabase providerDatabase;
        private AIProvider selectedProvider;
        private int selectedProviderIndex = 0;

        // UI State
        private int currentTab;
        private string[] tabNames = { "AI Generator", "Template Library", "Favorites", "Quick Actions" };

        // AI Generator
        private string commandInput = "";
        private string responseOutput = "";
        private bool isProcessing = false;
        private bool showConfig = false;

        // Template Library
        private string searchQuery = "";
        private string selectedCategory = "All";
        private List<CodeTemplate> searchResults = new List<CodeTemplate>();
        private int currentPage = 0;
        private const int itemsPerPage = 6;

        // Favorites
        private List<CodeTemplate> favorites = new List<CodeTemplate>();

        // UI Styles
        private GUIStyle headerStyle;
        private GUIStyle h1Style;
        private GUIStyle labelStyle;
        private GUIStyle linkStyle;
        private GUIStyle boxStyle;

        private static Rect headerRect;
        private static Rect bottomRect;
        private string helpMsg = "Welcome to Game Smith - AI-Powered Unity Development";
        #endregion

        #region Menu
        [MenuItem("Tools/Game Smith &G", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSmithWindow>("Game Smith");
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(1200, 900);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            // Load provider database
            providerDatabase = AIProviderManager.GetDatabase();
            selectedProvider = AIProviderManager.GetSelectedProvider();

            if (selectedProvider != null)
            {
                selectedProviderIndex = providerDatabase.GetProviderIndex(selectedProvider.providerName);
                config = selectedProvider.ToConfig();
            }
            else
            {
                config = AIAgentConfig.Load();
            }

            client = new AIAgentClient(config);
            searchResults = AITemplateLibrary.GetAllTemplates();
            InitializeStyles();
        }

        private void OnGUI()
        {
            DrawRects();
            DrawHeader();

            GUILayout.Space(5);
            DrawTabs();

            GUILayout.BeginArea(bottomRect);
            EditorGUILayout.HelpBox(helpMsg, MessageType.Info);
            GUILayout.EndArea();
        }
        #endregion

        #region Initialization
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            h1Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            linkStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = new Color(0.4f, 0.7f, 1f) },
                hover = { textColor = new Color(0.6f, 0.85f, 1f) },
                fontStyle = FontStyle.Bold
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        private static void DrawRects()
        {
            headerRect = new Rect(0, 0, Screen.width, 40);
            float bottomHeight = 45;
            bottomRect = new Rect(0, Screen.height - bottomHeight, Screen.width, bottomHeight);
        }
        #endregion

        #region Header & Layout
        private void DrawHeader()
        {
            DrawBox(headerRect, new Color(0.15f, 0.15f, 0.15f));
            GUILayout.BeginArea(headerRect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("⚒️ Game Smith", headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("?", GUILayout.Width(25), GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/muammar-yacoob/unity-gamesmith#readme");
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayout.Space(45);
        }

        private void DrawBox(Rect rect, Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            GUI.skin.box.normal.background = texture;
            GUI.Box(rect, GUIContent.none);
        }
        #endregion

        #region Tabs
        private void DrawTabs()
        {
            currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(28));

            float scrollViewHeight = position.height - headerRect.height - bottomRect.height - 80;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollViewHeight));

            switch (currentTab)
            {
                case 0: // AI Generator
                    helpMsg = "Generate Unity scripts using natural language commands or AI prompts";
                    DrawAIGeneratorTab();
                    break;
                case 1: // Template Library
                    helpMsg = $"Browse {searchResults.Count} pre-built templates - Search, filter, and use instantly";
                    DrawTemplateLibraryTab();
                    break;
                case 2: // Favorites
                    helpMsg = $"Your starred templates ({favorites.Count}) - Quick access to frequently used code";
                    DrawFavoritesTab();
                    break;
                case 3: // Quick Actions
                    helpMsg = "One-click generators for common game systems and mechanics";
                    DrawQuickActionsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region AI Generator Tab
        private void DrawAIGeneratorTab()
        {
            GUILayout.Space(10);

            // Collapsible Configuration Section
            showConfig = EditorGUILayout.BeginFoldoutHeaderGroup(showConfig,
                $"{(showConfig ? "▼" : "▶")} AI Configuration", h1Style);

            if (showConfig)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                // Provider Dropdown
                EditorGUILayout.LabelField("AI Provider", EditorStyles.boldLabel);

                if (providerDatabase == null || providerDatabase.providers.Count == 0)
                {
                    EditorGUILayout.HelpBox("No AI providers configured. Creating defaults...", MessageType.Warning);
                    providerDatabase = AIProviderManager.GetDatabase();
                }
                else
                {
                    string[] providerNames = providerDatabase.GetProviderNames();
                    int newIndex = EditorGUILayout.Popup("Select Provider", selectedProviderIndex, providerNames);

                    if (newIndex != selectedProviderIndex)
                    {
                        selectedProviderIndex = newIndex;
                        selectedProvider = providerDatabase.GetProviderByIndex(selectedProviderIndex);
                        if (selectedProvider != null)
                        {
                            config = selectedProvider.ToConfig();
                            client = new AIAgentClient(config);
                            AIProviderManager.SaveProviderSelection(selectedProvider.providerName);
                        }
                    }

                    GUILayout.Space(5);

                    // Show provider description
                    if (selectedProvider != null && !string.IsNullOrEmpty(selectedProvider.description))
                    {
                        EditorGUILayout.HelpBox(selectedProvider.description, MessageType.Info);
                    }

                    GUILayout.Space(5);

                    // API Key field (only if required)
                    if (selectedProvider != null && selectedProvider.requiresApiKey)
                    {
                        string newApiKey = EditorGUILayout.PasswordField("API Key", selectedProvider.apiKey);
                        if (newApiKey != selectedProvider.apiKey)
                        {
                            AIProviderManager.UpdateProviderApiKey(selectedProvider, newApiKey);
                            config.apiKey = newApiKey;
                            client = new AIAgentClient(config);
                        }

                        if (string.IsNullOrEmpty(selectedProvider.apiKey))
                        {
                            EditorGUILayout.HelpBox("⚠️ API Key required for this provider", MessageType.Warning);
                        }
                    }
                    else if (selectedProvider != null && selectedProvider.isLocal)
                    {
                        EditorGUILayout.HelpBox($"✓ Local provider - No API key needed", MessageType.Info);
                    }

                    GUILayout.Space(5);

                    // Advanced settings (collapsible)
                    if (selectedProvider != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Advanced", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Timeout: {selectedProvider.timeout}s | Temp: {selectedProvider.temperature:F2}", EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }

                    // Validation status
                    if (selectedProvider != null)
                    {
                        bool isValid = selectedProvider.IsValid();
                        if (!isValid)
                        {
                            EditorGUILayout.HelpBox($"❌ {selectedProvider.GetValidationMessage()}", MessageType.Error);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("✅ Provider configured correctly", MessageType.None);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(10);

            // Command Interface
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("💬 Natural Language Command", h1Style);
            commandInput = EditorGUILayout.TextArea(commandInput, GUILayout.Height(80));

            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(commandInput));
            if (GUILayout.Button(isProcessing ? "⏳ Processing..." : "🚀 Generate Code", GUILayout.Height(32)))
            {
                ExecuteCommand(commandInput);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Response Output
            if (!string.IsNullOrEmpty(responseOutput))
            {
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.LabelField("📝 AI Response", h1Style);
                EditorGUILayout.TextArea(responseOutput, GUILayout.Height(200));
                EditorGUILayout.EndVertical();
            }
        }
        #endregion

        #region Template Library Tab
        private void DrawTemplateLibraryTab()
        {
            GUILayout.Space(10);

            // Search & Filter Bar
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("🔍", GUILayout.Width(25));
            string newSearch = EditorGUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));
            if (newSearch != searchQuery)
            {
                searchQuery = newSearch;
                UpdateSearchResults();
                currentPage = 0;
            }

            if (GUILayout.Button("✕", GUILayout.Width(30)))
            {
                searchQuery = "";
                UpdateSearchResults();
            }

            EditorGUILayout.EndHorizontal();

            // Category Filter
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Category:", GUILayout.Width(70));
            var categories = AITemplateLibrary.GetCategories();
            int currentIndex = categories.IndexOf(selectedCategory);
            int newIndex = EditorGUILayout.Popup(currentIndex, categories.ToArray());

            if (newIndex != currentIndex)
            {
                selectedCategory = categories[newIndex];
                UpdateSearchResults();
                currentPage = 0;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Templates Grid
            DrawTemplateGrid(searchResults);

            // Pagination
            if (searchResults.Count > itemsPerPage)
            {
                DrawPagination();
            }
        }
        #endregion

        #region Favorites Tab
        private void DrawFavoritesTab()
        {
            GUILayout.Space(10);

            if (favorites.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No favorites yet.\n\nStar templates in the Template Library to add them here for quick access.",
                    MessageType.Info);
                return;
            }

            DrawTemplateGrid(favorites);
        }
        #endregion

        #region Quick Actions Tab
        private void DrawQuickActionsTab()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("🎮 Complete Systems", h1Style);

            if (GUILayout.Button("🎯 Generate 2D Shooter Project", GUILayout.Height(35)))
            {
                ExecuteCommand("Create a complete 2D top-down shooter with player, enemies, and shooting");
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("⚡ System Generators", h1Style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🏃 Player System", GUILayout.Height(30)))
            {
                PlayerSystemGenerator.GeneratePlayerSystem();
            }
            if (GUILayout.Button("👾 Enemy System", GUILayout.Height(30)))
            {
                EnemySystemGenerator.GenerateEnemySystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💥 Projectile System", GUILayout.Height(30)))
            {
                ProjectileSystemGenerator.GenerateProjectileSystem();
            }
            if (GUILayout.Button("📊 Level System", GUILayout.Height(30)))
            {
                LevelSystemGenerator.GenerateLevelSystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎨 UI System", GUILayout.Height(30)))
            {
                UISystemGenerator.GenerateUISystem();
            }
            if (GUILayout.Button("⚙️ Custom System", GUILayout.Height(30)))
            {
                currentTab = 0; // Switch to AI Generator tab
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Template Grid & Cards
        private void DrawTemplateGrid(List<CodeTemplate> templates)
        {
            if (templates == null || templates.Count == 0)
            {
                EditorGUILayout.HelpBox("No templates found", MessageType.Info);
                return;
            }

            int start = GetPageStart();
            int end = Mathf.Min(GetPageEnd(), templates.Count);

            const int columns = 2;
            float panelWidth = (position.width - 40) / columns;
            float panelHeight = 140f;

            for (int i = start; i < end; i += columns)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns && i + col < end; col++)
                {
                    DrawTemplateCard(templates[i + col], panelWidth, panelHeight);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8);
            }
        }

        private void DrawTemplateCard(CodeTemplate template, float width, float height)
        {
            Rect cardRect = GUILayoutUtility.GetRect(width - 10, height);

            // Card background
            EditorGUI.DrawRect(cardRect, new Color(0.22f, 0.22f, 0.22f, 1f));

            // Hover effect
            if (cardRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(cardRect, new Color(0.25f, 0.25f, 0.25f, 0.5f));
            }

            Rect contentRect = new Rect(cardRect.x + 8, cardRect.y + 8, cardRect.width - 16, cardRect.height - 16);
            GUILayout.BeginArea(contentRect);

            // Title
            GUIStyle titleStyle = new GUIStyle(h1Style) { fontSize = 13 };
            GUILayout.Label(template.name, titleStyle);

            // Description
            GUIStyle descStyle = new GUIStyle(labelStyle) { wordWrap = true, fontSize = 10 };
            GUILayout.Label(template.description, descStyle, GUILayout.Height(32));

            GUILayout.FlexibleSpace();

            // Meta info
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"📦 {template.category}", EditorStyles.miniLabel, GUILayout.Width(85));
            string complexity = new string('⭐', template.complexity);
            GUILayout.Label(complexity, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            // Action buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📋", GUILayout.Width(30), GUILayout.Height(24)))
            {
                GUIUtility.systemCopyBuffer = template.code;
                AIAgentLogger.LogSuccess($"Copied {template.name}");
            }

            if (GUILayout.Button("✨ Use", GUILayout.Height(24)))
            {
                UseTemplate(template);
            }

            string favIcon = favorites.Contains(template) ? "⭐" : "☆";
            if (GUILayout.Button(favIcon, GUILayout.Width(30), GUILayout.Height(24)))
            {
                ToggleFavorite(template);
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawPagination()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            int totalPages = Mathf.CeilToInt((float)searchResults.Count / itemsPerPage);

            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("◀", GUILayout.Width(40)))
            {
                currentPage--;
            }
            GUI.enabled = true;

            GUILayout.Label($"{currentPage + 1} / {totalPages}", EditorStyles.miniLabel, GUILayout.Width(60));

            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("▶", GUILayout.Width(40)))
            {
                currentPage++;
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Helper Methods
        private void UpdateSearchResults()
        {
            string category = selectedCategory == "All" ? null : selectedCategory;
            searchResults = AITemplateLibrary.SearchTemplates(searchQuery, category);
        }

        private void UseTemplate(CodeTemplate template)
        {
            ScriptGeneratorUtility.CreateScript(template.name.Replace(" ", ""), template.code);
            AIAgentLogger.LogSuccess($"Created: {template.name}");
            EditorUtility.DisplayDialog("Success", $"{template.name} created in Assets/Scripts/", "OK");
        }

        private void ToggleFavorite(CodeTemplate template)
        {
            if (favorites.Contains(template))
            {
                favorites.Remove(template);
                AIAgentLogger.Log($"Removed from favorites: {template.name}");
            }
            else
            {
                favorites.Add(template);
                AIAgentLogger.LogSuccess($"Added to favorites: {template.name}");
            }
            Repaint();
        }

        private int GetPageStart() => currentPage * itemsPerPage;
        private int GetPageEnd() => GetPageStart() + itemsPerPage;

        private void ExecuteCommand(string command)
        {
            if (isProcessing) return;

            isProcessing = true;
            responseOutput = "Processing...";
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
            return $@"You are a Unity game development expert. Generate C# code for:

{userCommand}

Requirements:
- Complete, production-ready Unity C# code
- All necessary using statements
- Helpful comments
- Follow Unity best practices
- Make it beginner-friendly

Provide only the C# code.";
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
        #endregion
    }
}
