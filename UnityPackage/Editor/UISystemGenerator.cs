using UnityEngine;
using UnityEditor;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Generates game UI and HUD systems
    /// </summary>
    public static class UISystemGenerator
    {
        [MenuItem("Tools/Unity AI Agent/Generate/UI System")]
        public static void GenerateUISystem()
        {
            GenerateHealthBar();
            GenerateScoreDisplay();
            GenerateGameOverScreen();

            EditorUtility.DisplayDialog("Success",
                "UI system generated!\n\nCreated scripts:\n- HealthBar\n- ScoreDisplay\n- GameOverScreen",
                "OK");
        }

        public static void GenerateHealthBar()
        {
            string script = @"using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(""Player"");
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            UpdateHealthBar(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}";
            ScriptGeneratorUtility.CreateScript("HealthBar", script);
        }

        public static void GenerateScoreDisplay()
        {
            string script = @"using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private int score = 0;

    private void Start()
    {
        UpdateScoreDisplay();

        // Subscribe to enemy deaths to increase score
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth enemy in enemies)
        {
            enemy.OnDeath.AddListener(() => AddScore(10));
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $""Score: {score}"";
        }
    }

    public int GetScore() => score;
}";
            ScriptGeneratorUtility.CreateScript("ScoreDisplay", script);
        }

        public static void GenerateGameOverScreen()
        {
            string script = @"using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag(""Player"");
        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnDeath.AddListener(ShowGameOver);
            }
        }
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;

            ScoreDisplay scoreDisplay = FindObjectOfType<ScoreDisplay>();
            if (scoreDisplay != null && scoreText != null)
            {
                scoreText.text = $""Final Score: {scoreDisplay.GetScore()}"";
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}";
            ScriptGeneratorUtility.CreateScript("GameOverScreen", script);
        }
    }
}
