using UnityEditor;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Auto-initializes GameSmith on Unity startup
    /// Ensures zero-setup experience for users
    /// </summary>
    [InitializeOnLoad]
    public static class GameSmithInitializer
    {
        private const string FIRST_RUN_KEY = "GameSmith_FirstRun";
        private const string HIDE_WELCOME_KEY = "GameSmith_HideWelcome";

        static GameSmithInitializer()
        {
            // Run on Unity startup
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            bool isFirstRun = !EditorPrefs.HasKey(FIRST_RUN_KEY);

            // Always ensure settings exist
            EnsureSettingsExist();

            // Show welcome window only on first run and if not disabled
            bool hideWelcome = EditorPrefs.GetBool(HIDE_WELCOME_KEY, false);
            if (isFirstRun && !hideWelcome)
            {
                EditorPrefs.SetBool(FIRST_RUN_KEY, true);
                EditorApplication.delayCall += ShowWelcomeWindow;
            }

            // Ensure providers.json is loaded
            GameSmithConfig.GetOrCreate();
        }

        private static void EnsureSettingsExist()
        {
            // This will create settings with defaults if they don't exist
            var settings = GameSmithSettings.Instance;

            // Log success (silent, only visible in console if user looks)
            if (settings != null)
            {
                Debug.Log("[GameSmith] ✓ Settings initialized successfully");
            }
        }

        private static void ShowWelcomeWindow()
        {
            GameSmithWelcomeWindow.ShowWindow();
        }

        [MenuItem("Tools/GameSmith/Reset First-Time Setup", false, 100)]
        private static void ResetFirstTimeSetup()
        {
            EditorPrefs.DeleteKey(FIRST_RUN_KEY);
            Debug.Log("[GameSmith] First-time setup reset. Restart Unity to see welcome window.");
        }
    }
}
