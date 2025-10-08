using UnityEngine;
using UnityEditor;

namespace SparkGames.UnityAIAgent.Editor
{
    /// <summary>
    /// Generates level management and wave spawning systems
    /// </summary>
    public static class LevelSystemGenerator
    {
        [MenuItem("Tools/Unity AI Agent/Generate/Level System")]
        public static void GenerateLevelSystem()
        {
            GenerateLevelManager();
            GenerateWaveSpawner();
            GenerateSpawnPoint();

            EditorUtility.DisplayDialog("Success",
                "Level system generated!\n\nCreated scripts:\n- LevelManager\n- WaveSpawner\n- SpawnPoint",
                "OK");
        }

        public static void GenerateLevelManager()
        {
            string script = @"using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header(""Level Settings"")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevels = 5;

    [Header(""References"")]
    [SerializeField] private WaveSpawner waveSpawner;

    private static LevelManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (waveSpawner == null)
            waveSpawner = FindObjectOfType<WaveSpawner>();

        StartLevel();
    }

    private void StartLevel()
    {
        Debug.Log($""Starting Level {currentLevel}"");

        if (waveSpawner != null)
        {
            waveSpawner.StartWaves(currentLevel);
        }
    }

    public void CompleteLevel()
    {
        currentLevel++;

        if (currentLevel > maxLevels)
        {
            Debug.Log(""Game Complete!"");
            // Handle game completion
        }
        else
        {
            StartLevel();
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public int GetCurrentLevel() => currentLevel;
    public int GetMaxLevels() => maxLevels;
}";
            ScriptGeneratorUtility.CreateScript("LevelManager", script);
        }

        public static void GenerateWaveSpawner()
        {
            string script = @"using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public GameObject enemyPrefab;
        public int count;
        public float spawnInterval = 0.5f;
    }

    [Header(""Wave Settings"")]
    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float difficultyMultiplier = 1.5f;

    [Header(""Spawn Points"")]
    [SerializeField] private Transform[] spawnPoints;

    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;

    public void StartWaves(int levelNumber)
    {
        // Adjust difficulty based on level
        AdjustDifficultyForLevel(levelNumber);
        StartCoroutine(SpawnWaves());
    }

    private void AdjustDifficultyForLevel(int level)
    {
        foreach (Wave wave in waves)
        {
            wave.count = Mathf.RoundToInt(wave.count * Mathf.Pow(difficultyMultiplier, level - 1));
        }
    }

    private IEnumerator SpawnWaves()
    {
        foreach (Wave wave in waves)
        {
            currentWaveIndex++;
            Debug.Log($""Starting Wave {currentWaveIndex}"");

            yield return StartCoroutine(SpawnWave(wave));

            // Wait for all enemies to be defeated
            while (enemiesRemaining > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log(""All waves complete!"");
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.CompleteLevel();
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        for (int i = 0; i < wave.count; i++)
        {
            SpawnEnemy(wave.enemyPrefab);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning(""No spawn points assigned!"");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath.AddListener(() => enemiesRemaining--);
            enemiesRemaining++;
        }
    }
}";
            ScriptGeneratorUtility.CreateScript("WaveSpawner", script);
        }

        public static void GenerateSpawnPoint()
        {
            string script = @"using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = Color.green;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}";
            ScriptGeneratorUtility.CreateScript("SpawnPoint", script);
        }
    }
}
