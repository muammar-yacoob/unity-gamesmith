using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SparkGames.UnityAIAgent.Editor
{
    /// <summary>
    /// [DEPRECATED] Legacy Enhanced window - Use Tools/Game Smith instead
    /// </summary>
    [System.Obsolete("This window is deprecated. Please use Tools/Game Smith instead.")]
    public class EnhancedAIAgentWindow : EditorWindow
    {
        private AIAgentConfig config;
        private AIAgentClient client;
        private Vector2 scrollPosition;
        private string commandInput = "";
        private string responseOutput = "";
        private bool isProcessing = false;

        // Search/Browse functionality
        private string searchQuery = "";
        private string selectedCategory = "All";
        private List<CodeTemplate> searchResults = new List<CodeTemplate>();
        private List<CodeTemplate> favorites = new List<CodeTemplate>();

        // Pagination
        private int currentPage = 0;
        private const int itemsPerPage = 6;

        // UI State
        private int selectedTab = 0;
        private string[] tabNames = { "AI Generator", "Template Library", "Favorites" };
        private CodeTemplate selectedTemplate;

        [MenuItem("Tools/Legacy/Unity AI Agent Enhanced (Deprecated)", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<EnhancedAIAgentWindow>("Unity AI Agent");
            window.minSize = new Vector2(700, 700);
            window.maxSize = new Vector2(1200, 1000);
        }

        private void OnEnable()
        {
            config = AIAgentConfig.Load();
            client = new AIAgentClient(config);
            searchResults = AITemplateLibrary.GetAllTemplates();
        }

        private void OnGUI()
        {
            DrawHeader();

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: // AI Generator
                    DrawAIGeneratorTab();
                    break;
                case 1: // Template Library
                    DrawTemplateLibraryTab();
                    break;
                case 2: // Favorites
                    DrawFavoritesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("⚡ Unity AI Agent", headerStyle);
            EditorGUILayout.LabelField("AI-Powered Game Development Toolkit", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(10);
        }

        private void DrawAIGeneratorTab()
        {
            DrawConfiguration();
            DrawQuickActions();
            DrawCommandInterface();
            DrawResponse();
        }

        private void DrawTemplateLibraryTab()
        {
            EditorGUILayout.BeginVertical("box");

            // Search Bar
            DrawSearchBar();

            // Category Filter
            DrawCategoryFilter();

            // Results Count
            DrawResultsInfo();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Grid Display
            DrawTemplateGrid();

            // Pagination
            DrawPagination();
        }

        private void DrawFavoritesTab()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"⭐ Favorite Templates ({favorites.Count})", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            if (favorites.Count == 0)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("No favorites yet. Star templates in the Template Library to add them here.", MessageType.Info);
                return;
            }

            GUILayout.Space(10);
            DrawTemplateGrid(favorites);
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🔍 Search:", GUILayout.Width(80));

            string newSearch = EditorGUILayout.TextField(searchQuery);
            if (newSearch != searchQuery)
            {
                searchQuery = newSearch;
                UpdateSearchResults();
                currentPage = 0;
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchQuery = "";
                UpdateSearchResults();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryFilter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Category:", GUILayout.Width(80));

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
        }

        private void DrawResultsInfo()
        {
            string resultText = searchResults.Count == 0 ? "No templates found" :
                $"Showing {GetPageStart() + 1}-{Mathf.Min(GetPageEnd(), searchResults.Count)} of {searchResults.Count} templates";
            EditorGUILayout.LabelField(resultText, EditorStyles.miniLabel);
        }

        private void DrawTemplateGrid(List<CodeTemplate> templates = null)
        {
            templates = templates ?? searchResults;

            int start = GetPageStart();
            int end = Mathf.Min(GetPageEnd(), templates.Count);

            const int columns = 2;
            float panelWidth = (position.width - 40) / columns;
            float panelHeight = 150f;

            for (int i = start; i < end; i += columns)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns && i + col < end; col++)
                {
                    DrawTemplateCard(templates[i + col], panelWidth, panelHeight);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }

        private void DrawTemplateCard(CodeTemplate template, float width, float height)
        {
            Rect cardRect = GUILayoutUtility.GetRect(width - 10, height);

            // Background
            EditorGUI.DrawRect(cardRect, new Color(0.2f, 0.2f, 0.2f, 1f));

            // Content area
            Rect contentRect = new Rect(cardRect.x + 5, cardRect.y + 5, cardRect.width - 10, cardRect.height - 10);

            GUILayout.BeginArea(contentRect);

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUILayout.Label(template.name, titleStyle);

            // Description
            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                normal = { textColor = Color.gray }
            };
            GUILayout.Label(template.description, descStyle, GUILayout.Height(40));

            GUILayout.FlexibleSpace();

            // Tags
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"📦 {template.category}", EditorStyles.miniLabel, GUILayout.Width(80));
            GUILayout.Label($"⭐ {template.complexity}/5", EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📋 Copy Code", GUILayout.Height(25)))
            {
                GUIUtility.systemCopyBuffer = template.code;
                AIAgentLogger.LogSuccess($"Copied {template.name} to clipboard");
            }

            if (GUILayout.Button("✨ Use Template", GUILayout.Height(25)))
            {
                UseTemplate(template);
            }

            // Favorite button
            string favIcon = favorites.Contains(template) ? "⭐" : "☆";
            if (GUILayout.Button(favIcon, GUILayout.Width(30), GUILayout.Height(25)))
            {
                ToggleFavorite(template);
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawPagination()
        {
            if (searchResults.Count <= itemsPerPage) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            int totalPages = Mathf.CeilToInt((float)searchResults.Count / itemsPerPage);

            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("◀ Previous", GUILayout.Width(100)))
            {
                currentPage--;
            }
            GUI.enabled = true;

            GUILayout.Label($"Page {currentPage + 1} / {totalPages}", EditorStyles.miniLabel, GUILayout.Width(80));

            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("Next ▶", GUILayout.Width(100)))
            {
                currentPage++;
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🤖 AI Configuration", EditorStyles.boldLabel);

            config.apiUrl = EditorGUILayout.TextField("API URL", config.apiUrl);
            config.model = EditorGUILayout.TextField("Model", config.model);
            config.apiKey = EditorGUILayout.PasswordField("API Key", config.apiKey);
            config.timeout = EditorGUILayout.IntField("Timeout (seconds)", config.timeout);
            config.temperature = EditorGUILayout.Slider("Temperature", config.temperature, 0f, 1f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Save Config"))
            {
                config.Save();
                EditorUtility.DisplayDialog("Success", "Configuration saved!", "OK");
            }
            if (GUILayout.Button("📂 Load Config"))
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
            EditorGUILayout.LabelField("⚡ Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("🎮 Create 2D Shooter Project", GUILayout.Height(35)))
            {
                ExecuteCommand("Create a complete 2D top-down shooter with player, enemies, and shooting");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🏃 Player System"))
            {
                PlayerSystemGenerator.GeneratePlayerSystem();
            }
            if (GUILayout.Button("👾 Enemy System"))
            {
                EnemySystemGenerator.GenerateEnemySystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💥 Projectiles"))
            {
                ProjectileSystemGenerator.GenerateProjectileSystem();
            }
            if (GUILayout.Button("📊 Level System"))
            {
                LevelSystemGenerator.GenerateLevelSystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawCommandInterface()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("💬 Natural Language Commands", EditorStyles.boldLabel);

            commandInput = EditorGUILayout.TextArea(commandInput, GUILayout.Height(80));

            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(commandInput));
            if (GUILayout.Button(isProcessing ? "⏳ Processing..." : "🚀 Execute Command", GUILayout.Height(30)))
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
            EditorGUILayout.LabelField("📝 AI Response", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(responseOutput))
            {
                EditorGUILayout.TextArea(responseOutput, GUILayout.Height(200));
            }
            else
            {
                EditorGUILayout.HelpBox("Execute a command to see AI response", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void UpdateSearchResults()
        {
            string category = selectedCategory == "All" ? null : selectedCategory;
            searchResults = AITemplateLibrary.SearchTemplates(searchQuery, category);
        }

        private void UseTemplate(CodeTemplate template)
        {
            ScriptGeneratorUtility.CreateScript(template.name.Replace(" ", ""), template.code);
            AIAgentLogger.LogSuccess($"Created script: {template.name}");
            EditorUtility.DisplayDialog("Success", $"{template.name} script created in Assets/Scripts/", "OK");
        }

        private void ToggleFavorite(CodeTemplate template)
        {
            if (favorites.Contains(template))
            {
                favorites.Remove(template);
            }
            else
            {
                favorites.Add(template);
            }
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
    }
}
