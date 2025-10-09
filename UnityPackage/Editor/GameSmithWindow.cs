using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Unified Game Smith Editor Window - Modern UI Toolkit implementation
    /// </summary>
    public class GameSmithWindow : EditorWindow
    {
        #region Fields
        private AIAgentConfig config;
        private AIAgentClient client;

        // AI Provider System
        private AIProviderDatabase providerDatabase;
        private AIProvider selectedProvider;

        // Template Library
        private string searchQuery = "";
        private string selectedCategory = "All";
        private List<CodeTemplate> searchResults = new List<CodeTemplate>();
        private int currentPage = 0;
        private const int itemsPerPage = 8;

        // Favorites
        private List<CodeTemplate> favorites = new List<CodeTemplate>();

        // UI State
        private int currentTab = 0;
        private bool isProcessing = false;

        // UI Elements References
        private VisualElement root;
        private Button[] tabButtons;
        private VisualElement[] tabContents;
        private Label helpMessage;
        private TextField commandInput;
        private TextField responseOutput;
        private VisualElement responseContainer;
        private PopupField<string> providerDropdown;
        private PopupField<string> categoryDropdown;
        private VisualElement templatesContainer;
        private VisualElement favoritesContainer;
        private Label pageLabel;
        private Button prevPageButton;
        private Button nextPageButton;
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
        public void CreateGUI()
        {
            // Load provider database
            providerDatabase = AIProviderManager.GetDatabase();
            selectedProvider = AIProviderManager.GetSelectedProvider();

            if (selectedProvider != null)
            {
                config = selectedProvider.ToConfig();
            }
            else
            {
                config = AIAgentConfig.Load();
            }

            client = new AIAgentClient(config);
            searchResults = AITemplateLibrary.GetAllTemplates();

            // Find UXML and USS files dynamically
            string[] uxmlGuids = AssetDatabase.FindAssets("GameSmithWindow t:VisualTreeAsset");
            string[] ussGuids = AssetDatabase.FindAssets("GameSmithWindow t:StyleSheet");

            VisualTreeAsset visualTree = null;
            StyleSheet styleSheet = null;

            if (uxmlGuids.Length > 0)
            {
                string uxmlPath = AssetDatabase.GUIDToAssetPath(uxmlGuids[0]);
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            }

            if (ussGuids.Length > 0)
            {
                string ussPath = AssetDatabase.GUIDToAssetPath(ussGuids[0]);
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            }

            if (visualTree == null)
            {
                Debug.LogError("Could not find GameSmithWindow.uxml. Make sure it exists in the Editor folder.");
                CreateFallbackUI();
                return;
            }

            root = visualTree.CloneTree();
            rootVisualElement.Add(root);

            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning("Could not find GameSmithWindow.uss. Styles may not be applied.");
            }

            InitializeUIReferences();
            SetupCallbacks();
            UpdateProviderUI();
        }

        private void CreateFallbackUI()
        {
            var fallbackLabel = new Label("Game Smith requires UXML and USS files.\n\nPlease ensure GameSmithWindow.uxml and GameSmithWindow.uss exist in the Editor folder.");
            fallbackLabel.style.paddingLeft = 20;
            fallbackLabel.style.paddingRight = 20;
            fallbackLabel.style.paddingTop = 20;
            fallbackLabel.style.paddingBottom = 20;
            fallbackLabel.style.whiteSpace = WhiteSpace.Normal;
            fallbackLabel.style.fontSize = 14;
            rootVisualElement.Add(fallbackLabel);
        }
        #endregion

        #region Initialization
        private void InitializeUIReferences()
        {
            // Header
            var helpButton = root.Q<Button>("help-button");
            if (helpButton != null)
            {
                helpButton.clicked += () => Application.OpenURL("https://github.com/muammar-yacoob/unity-gamesmith#readme");
            }

            // Tabs
            tabButtons = new Button[]
            {
                root.Q<Button>("tab-ai-generator"),
                root.Q<Button>("tab-template-library"),
                root.Q<Button>("tab-favorites"),
                root.Q<Button>("tab-quick-actions")
            };

            tabContents = new VisualElement[]
            {
                root.Q<VisualElement>("ai-generator-tab"),
                root.Q<VisualElement>("template-library-tab"),
                root.Q<VisualElement>("favorites-tab"),
                root.Q<VisualElement>("quick-actions-tab")
            };

            // Footer
            helpMessage = root.Q<Label>("help-message");

            // AI Generator Tab
            commandInput = root.Q<TextField>("command-input");
            responseOutput = root.Q<TextField>("response-output");
            responseContainer = root.Q<VisualElement>("response-container");

            // Provider dropdown
            var providerNames = providerDatabase?.GetProviderNames() ?? new string[] { };
            providerDropdown = root.Q<PopupField<string>>("provider-dropdown");
            if (providerDropdown != null)
            {
                providerDropdown.choices = new List<string>(providerNames);
                if (selectedProvider != null && providerNames.Length > 0)
                {
                    int providerIndex = providerDatabase.GetProviderIndex(selectedProvider.providerName);
                    providerDropdown.index = providerIndex;
                }
            }

            // Template Library Tab
            var categories = AITemplateLibrary.GetCategories();
            categoryDropdown = root.Q<PopupField<string>>("category-dropdown");
            if (categoryDropdown != null)
            {
                categoryDropdown.choices = categories;
                categoryDropdown.value = selectedCategory;
            }

            templatesContainer = root.Q<VisualElement>("templates-container");
            pageLabel = root.Q<Label>("page-label");
            prevPageButton = root.Q<Button>("prev-page-button");
            nextPageButton = root.Q<Button>("next-page-button");

            // Favorites Tab
            favoritesContainer = root.Q<VisualElement>("favorites-container");
        }

        private void SetupCallbacks()
        {
            // Tab buttons
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                tabButtons[i].clicked += () => SwitchTab(tabIndex);
            }

            // AI Generator
            var executeButton = root.Q<Button>("execute-button");
            executeButton.clicked += ExecuteCommand;

            // Provider selection
            providerDropdown.RegisterValueChangedCallback(evt => OnProviderChanged(evt.newValue));

            // Template Library
            var searchField = root.Q<TextField>("search-field");
            searchField.RegisterValueChangedCallback(evt => OnSearchChanged(evt.newValue));

            var clearSearchButton = root.Q<Button>("clear-search-button");
            clearSearchButton.clicked += () => {
                searchField.value = "";
                OnSearchChanged("");
            };

            categoryDropdown.RegisterValueChangedCallback(evt => OnCategoryChanged(evt.newValue));

            // Pagination
            prevPageButton.clicked += () => ChangePage(-1);
            nextPageButton.clicked += () => ChangePage(1);

            // Quick Actions
            var generateShooterButton = root.Q<Button>("generate-shooter-button");
            generateShooterButton.clicked += () => ExecuteQuickCommand("Create a complete 2D top-down shooter with player, enemies, and shooting");

            var playerSystemButton = root.Q<Button>("player-system-button");
            playerSystemButton.clicked += () => PlayerSystemGenerator.GeneratePlayerSystem();

            var enemySystemButton = root.Q<Button>("enemy-system-button");
            enemySystemButton.clicked += () => EnemySystemGenerator.GenerateEnemySystem();

            var projectileSystemButton = root.Q<Button>("projectile-system-button");
            projectileSystemButton.clicked += () => ProjectileSystemGenerator.GenerateProjectileSystem();

            var levelSystemButton = root.Q<Button>("level-system-button");
            levelSystemButton.clicked += () => LevelSystemGenerator.GenerateLevelSystem();

            var uiSystemButton = root.Q<Button>("ui-system-button");
            uiSystemButton.clicked += () => UISystemGenerator.GenerateUISystem();

            var customSystemButton = root.Q<Button>("custom-system-button");
            customSystemButton.clicked += () => SwitchTab(0); // Switch to AI Generator
        }

        private void UpdateProviderUI()
        {
            var providerDescription = root.Q<VisualElement>("provider-description");
            var providerDescriptionText = root.Q<Label>("provider-description-text");
            var apiKeyContainer = root.Q<VisualElement>("api-key-container");
            var apiKeyField = root.Q<TextField>("api-key-field");
            var validationStatus = root.Q<VisualElement>("validation-status");
            var validationMessage = root.Q<Label>("validation-message");

            if (selectedProvider != null)
            {
                // Show description
                if (!string.IsNullOrEmpty(selectedProvider.description))
                {
                    providerDescription.style.display = DisplayStyle.Flex;
                    providerDescriptionText.text = selectedProvider.description;
                }
                else
                {
                    providerDescription.style.display = DisplayStyle.None;
                }

                // Show API key field if required
                if (selectedProvider.requiresApiKey)
                {
                    apiKeyContainer.style.display = DisplayStyle.Flex;
                    apiKeyField.value = selectedProvider.apiKey;
                    apiKeyField.RegisterValueChangedCallback(evt => OnApiKeyChanged(evt.newValue));
                }
                else
                {
                    apiKeyContainer.style.display = DisplayStyle.None;
                }

                // Show validation status
                validationStatus.style.display = DisplayStyle.Flex;
                bool isValid = selectedProvider.IsValid();
                validationMessage.text = isValid ? "✅ Provider configured correctly" : $"❌ {selectedProvider.GetValidationMessage()}";

                // Update style based on validation
                validationStatus.RemoveFromClassList("help-box--error");
                validationStatus.RemoveFromClassList("help-box--success");
                validationStatus.AddToClassList(isValid ? "help-box--success" : "help-box--error");
            }
        }
        #endregion

        #region Tab Management
        private void SwitchTab(int tabIndex)
        {
            currentTab = tabIndex;

            // Update tab buttons
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (i == tabIndex)
                {
                    tabButtons[i].AddToClassList("tab-button--selected");
                }
                else
                {
                    tabButtons[i].RemoveFromClassList("tab-button--selected");
                }
            }

            // Update tab contents
            for (int i = 0; i < tabContents.Length; i++)
            {
                tabContents[i].style.display = (i == tabIndex) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Update help message
            string[] helpMessages = new string[]
            {
                "Generate Unity scripts using natural language commands or AI prompts",
                $"Browse {searchResults.Count} pre-built templates - Search, filter, and use instantly",
                $"Your starred templates ({favorites.Count}) - Quick access to frequently used code",
                "One-click generators for common game systems and mechanics"
            };
            helpMessage.text = helpMessages[tabIndex];

            // Load content for specific tabs
            if (tabIndex == 1) // Template Library
            {
                UpdateTemplateGrid();
            }
            else if (tabIndex == 2) // Favorites
            {
                UpdateFavoritesGrid();
            }
        }
        #endregion

        #region AI Generator
        private void ExecuteCommand()
        {
            if (isProcessing || string.IsNullOrWhiteSpace(commandInput.value)) return;

            isProcessing = true;
            responseContainer.style.display = DisplayStyle.Flex;
            responseOutput.value = "Processing...";

            var executeButton = root.Q<Button>("execute-button");
            executeButton.text = "⏳ Processing...";
            executeButton.SetEnabled(false);

            EditorCoroutineUtility.StartCoroutine(
                client.SendPromptAsync(
                    GeneratePrompt(commandInput.value),
                    OnSuccess,
                    OnError
                ),
                this
            );
        }

        private void ExecuteQuickCommand(string command)
        {
            commandInput.value = command;
            SwitchTab(0); // Switch to AI Generator
            ExecuteCommand();
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
            responseOutput.value = response;

            var executeButton = root.Q<Button>("execute-button");
            executeButton.text = "🚀 Generate Code";
            executeButton.SetEnabled(true);
        }

        private void OnError(string error)
        {
            isProcessing = false;
            responseOutput.value = $"Error: {error}";
            EditorUtility.DisplayDialog("Error", error, "OK");

            var executeButton = root.Q<Button>("execute-button");
            executeButton.text = "🚀 Generate Code";
            executeButton.SetEnabled(true);
        }
        #endregion

        #region Provider Management
        private void OnProviderChanged(string providerName)
        {
            selectedProvider = providerDatabase.GetProviderByName(providerName);
            if (selectedProvider != null)
            {
                config = selectedProvider.ToConfig();
                client = new AIAgentClient(config);
                AIProviderManager.SaveProviderSelection(selectedProvider.providerName);
                UpdateProviderUI();
            }
        }

        private void OnApiKeyChanged(string newApiKey)
        {
            if (selectedProvider != null && newApiKey != selectedProvider.apiKey)
            {
                AIProviderManager.UpdateProviderApiKey(selectedProvider, newApiKey);
                config.apiKey = newApiKey;
                client = new AIAgentClient(config);
                UpdateProviderUI();
            }
        }
        #endregion

        #region Template Library
        private void OnSearchChanged(string newSearch)
        {
            searchQuery = newSearch;
            UpdateSearchResults();
            currentPage = 0;
            UpdateTemplateGrid();
        }

        private void OnCategoryChanged(string newCategory)
        {
            selectedCategory = newCategory;
            UpdateSearchResults();
            currentPage = 0;
            UpdateTemplateGrid();
        }

        private void UpdateSearchResults()
        {
            string category = selectedCategory == "All" ? null : selectedCategory;
            searchResults = AITemplateLibrary.SearchTemplates(searchQuery, category);
        }

        private void UpdateTemplateGrid()
        {
            templatesContainer.Clear();

            if (searchResults == null || searchResults.Count == 0)
            {
                var helpBox = new VisualElement();
                helpBox.AddToClassList("help-box");
                var label = new Label("No templates found");
                helpBox.Add(label);
                templatesContainer.Add(helpBox);
                return;
            }

            int start = currentPage * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, searchResults.Count);

            for (int i = start; i < end; i++)
            {
                var templateCard = CreateTemplateCard(searchResults[i]);
                templatesContainer.Add(templateCard);
            }

            UpdatePagination();
        }

        private VisualElement CreateTemplateCard(CodeTemplate template)
        {
            var card = new VisualElement();
            card.AddToClassList("template-card");

            // Title
            var title = new Label(template.name);
            title.AddToClassList("template-title");
            card.Add(title);

            // Description
            var description = new Label(template.description);
            description.AddToClassList("template-description");
            card.Add(description);

            // Meta info
            var meta = new VisualElement();
            meta.AddToClassList("template-meta");

            var category = new Label($"📦 {template.category}");
            category.AddToClassList("template-category");
            meta.Add(category);

            var complexity = new Label(new string('⭐', template.complexity));
            complexity.AddToClassList("template-category");
            meta.Add(complexity);

            card.Add(meta);

            // Actions
            var actions = new VisualElement();
            actions.AddToClassList("template-actions");

            var copyButton = new Button(() => {
                GUIUtility.systemCopyBuffer = template.code;
                AIAgentLogger.LogSuccess($"Copied {template.name}");
            });
            copyButton.text = "📋 Copy";
            copyButton.AddToClassList("template-button");
            actions.Add(copyButton);

            var useButton = new Button(() => UseTemplate(template));
            useButton.text = "✨ Use Template";
            useButton.AddToClassList("template-button");
            actions.Add(useButton);

            var favButton = new Button(() => ToggleFavorite(template));
            favButton.text = favorites.Contains(template) ? "⭐" : "☆";
            favButton.AddToClassList("favorite-button");
            if (favorites.Contains(template))
            {
                favButton.AddToClassList("favorite-button--active");
            }
            actions.Add(favButton);

            card.Add(actions);

            return card;
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

            if (currentTab == 1) // Template Library
            {
                UpdateTemplateGrid();
            }
            else if (currentTab == 2) // Favorites
            {
                UpdateFavoritesGrid();
            }
        }

        private void UpdatePagination()
        {
            int totalPages = Mathf.CeilToInt((float)searchResults.Count / itemsPerPage);
            pageLabel.text = $"{currentPage + 1} / {totalPages}";
            prevPageButton.SetEnabled(currentPage > 0);
            nextPageButton.SetEnabled(currentPage < totalPages - 1);
        }

        private void ChangePage(int direction)
        {
            int totalPages = Mathf.CeilToInt((float)searchResults.Count / itemsPerPage);
            currentPage = Mathf.Clamp(currentPage + direction, 0, totalPages - 1);
            UpdateTemplateGrid();
        }
        #endregion

        #region Favorites
        private void UpdateFavoritesGrid()
        {
            favoritesContainer.Clear();

            var emptyMessage = root.Q<VisualElement>("favorites-empty");
            if (favorites.Count == 0)
            {
                emptyMessage.style.display = DisplayStyle.Flex;
                return;
            }

            emptyMessage.style.display = DisplayStyle.None;

            foreach (var template in favorites)
            {
                var templateCard = CreateTemplateCard(template);
                favoritesContainer.Add(templateCard);
            }
        }
        #endregion
    }
}
