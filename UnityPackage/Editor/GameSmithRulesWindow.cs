using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple editor window for editing custom AI rules
    /// </summary>
    public class GameSmithRulesWindow : EditorWindow
    {
        private TextField rulesTextField;
        private string currentRules;
        private bool isDirty;

        [MenuItem("Tools/GameSmith/Edit AI Rules", false, 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSmithRulesWindow>("AI Rules");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        public void CreateGUI()
        {
            // Load current rules
            currentRules = CustomAIRules.GetRules();

            // Root container
            var root = rootVisualElement;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;

            // Header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 8;
            header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.4f, 0.7f, 1f, 0.3f);

            var titleLabel = new Label("Custom AI Rules");
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.95f, 0.95f, 0.95f);

            var helpLabel = new Label("These rules are appended to the base system prompt");
            helpLabel.style.fontSize = 10;
            helpLabel.style.color = new Color(0.7f, 0.7f, 0.7f);

            var titleContainer = new VisualElement();
            titleContainer.Add(titleLabel);
            titleContainer.Add(helpLabel);

            header.Add(titleContainer);
            root.Add(header);

            // Text field
            rulesTextField = new TextField();
            rulesTextField.multiline = true;
            rulesTextField.value = currentRules;
            rulesTextField.style.flexGrow = 1;
            rulesTextField.style.fontSize = 12;
            rulesTextField.style.unityTextAlign = TextAnchor.UpperLeft;
            rulesTextField.style.whiteSpace = WhiteSpace.Normal;
            rulesTextField.style.marginBottom = 8;

            // Add placeholder styling when empty
            if (string.IsNullOrWhiteSpace(currentRules))
            {
                rulesTextField.value = "# Add custom AI rules here...\n\n" +
                    "Examples:\n" +
                    "- Always use PascalCase for class names\n" +
                    "- Prefer async/await with UniTask\n" +
                    "- Follow Unity naming conventions\n" +
                    "- Test gameplay frequently with unity_enter_play_mode";
            }

            rulesTextField.RegisterValueChangedCallback(evt =>
            {
                isDirty = evt.newValue != currentRules;
            });

            root.Add(rulesTextField);

            // Footer buttons
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.marginTop = 4;

            var openExternalButton = new Button(OpenInExternalEditor);
            openExternalButton.text = "Open in Text Editor";
            openExternalButton.style.marginRight = 8;
            openExternalButton.style.paddingLeft = 12;
            openExternalButton.style.paddingRight = 12;
            openExternalButton.style.paddingTop = 6;
            openExternalButton.style.paddingBottom = 6;

            var saveButton = new Button(SaveRules);
            saveButton.text = "Save";
            saveButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
            saveButton.style.color = Color.white;
            saveButton.style.paddingLeft = 20;
            saveButton.style.paddingRight = 20;
            saveButton.style.paddingTop = 6;
            saveButton.style.paddingBottom = 6;
            saveButton.style.borderTopLeftRadius = 4;
            saveButton.style.borderTopRightRadius = 4;
            saveButton.style.borderBottomLeftRadius = 4;
            saveButton.style.borderBottomRightRadius = 4;

            footer.Add(openExternalButton);
            footer.Add(saveButton);
            root.Add(footer);

            // Focus text field
            EditorApplication.delayCall += () => rulesTextField.Focus();
        }

        private void SaveRules()
        {
            CustomAIRules.SaveRules(rulesTextField.value);
            currentRules = rulesTextField.value;
            isDirty = false;

            // Clear system prompt cache to reload with new rules
            SystemPrompts.ClearCache();

            ShowNotification(new GUIContent("✓ Rules saved"));
        }

        private void OpenInExternalEditor()
        {
            // Save current changes first
            if (isDirty)
            {
                if (EditorUtility.DisplayDialog("Save Changes?",
                    "Save changes before opening in external editor?",
                    "Save", "Discard"))
                {
                    SaveRules();
                }
            }

            CustomAIRules.OpenInExternalEditor();
        }

        private void OnDestroy()
        {
            if (isDirty)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes. Save before closing?",
                    "Save", "Discard"))
                {
                    SaveRules();
                }
            }
        }
    }
}
