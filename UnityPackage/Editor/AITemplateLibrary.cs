using System;
using System.Collections.Generic;
using UnityEngine;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Library of pre-built code templates and examples
    /// </summary>
    [Serializable]
    public class CodeTemplate
    {
        public string id;
        public string name;
        public string description;
        public string category;
        public string[] tags;
        public string code;
        public Texture2D thumbnail;
        public int complexity; // 1-5
        public bool isFavorite;
    }

    public static class AITemplateLibrary
    {
        private static List<CodeTemplate> templates;

        public static List<CodeTemplate> GetAllTemplates()
        {
            if (templates == null)
            {
                InitializeTemplates();
            }
            return templates;
        }

        public static List<CodeTemplate> SearchTemplates(string searchQuery, string category = null)
        {
            var allTemplates = GetAllTemplates();
            var results = new List<CodeTemplate>();

            searchQuery = searchQuery?.ToLower() ?? "";

            foreach (var template in allTemplates)
            {
                bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                    template.name.ToLower().Contains(searchQuery) ||
                    template.description.ToLower().Contains(searchQuery) ||
                    Array.Exists(template.tags, tag => tag.ToLower().Contains(searchQuery));

                bool matchesCategory = string.IsNullOrEmpty(category) || template.category == category;

                if (matchesSearch && matchesCategory)
                {
                    results.Add(template);
                }
            }

            return results;
        }

        public static List<string> GetCategories()
        {
            return new List<string>
            {
                "All",
                "Player",
                "Enemy",
                "Projectile",
                "UI",
                "Level",
                "Camera",
                "Audio",
                "Power-ups",
                "Effects"
            };
        }

        private static void InitializeTemplates()
        {
            templates = new List<CodeTemplate>
            {
                new CodeTemplate
                {
                    id = "player_controller_2d",
                    name = "2D Player Controller",
                    description = "WASD movement with mouse aiming for top-down games",
                    category = "Player",
                    tags = new[] { "movement", "2d", "topdown", "player" },
                    complexity = 2,
                    code = GeneratePlayerControllerCode()
                },
                new CodeTemplate
                {
                    id = "enemy_chase_ai",
                    name = "Chase Enemy AI",
                    description = "Enemy that detects and chases the player",
                    category = "Enemy",
                    tags = new[] { "ai", "enemy", "chase", "detection" },
                    complexity = 2,
                    code = GenerateEnemyAICode()
                },
                new CodeTemplate
                {
                    id = "shooting_system",
                    name = "Shooting System",
                    description = "Projectile-based shooting with cooldown",
                    category = "Projectile",
                    tags = new[] { "shooting", "projectile", "weapon" },
                    complexity = 2,
                    code = GenerateShootingCode()
                },
                new CodeTemplate
                {
                    id = "health_system",
                    name = "Health System",
                    description = "Health management with damage and healing",
                    category = "Player",
                    tags = new[] { "health", "damage", "healing" },
                    complexity = 1,
                    code = GenerateHealthCode()
                },
                new CodeTemplate
                {
                    id = "wave_spawner",
                    name = "Wave Spawner",
                    description = "Spawn enemies in waves with increasing difficulty",
                    category = "Level",
                    tags = new[] { "spawner", "waves", "enemies" },
                    complexity = 3,
                    code = GenerateWaveSpawnerCode()
                },
                new CodeTemplate
                {
                    id = "health_bar_ui",
                    name = "Health Bar UI",
                    description = "Dynamic health bar that updates with player health",
                    category = "UI",
                    tags = new[] { "ui", "healthbar", "hud" },
                    complexity = 1,
                    code = GenerateHealthBarCode()
                },
                new CodeTemplate
                {
                    id = "camera_follow",
                    name = "Camera Follow",
                    description = "Smooth camera that follows the player",
                    category = "Camera",
                    tags = new[] { "camera", "follow", "smooth" },
                    complexity = 2,
                    code = GenerateCameraFollowCode()
                },
                new CodeTemplate
                {
                    id = "dash_ability",
                    name = "Dash Ability",
                    description = "Quick dash movement with cooldown",
                    category = "Player",
                    tags = new[] { "dash", "ability", "movement" },
                    complexity = 2,
                    code = GenerateDashCode()
                },
                new CodeTemplate
                {
                    id = "powerup_pickup",
                    name = "Power-up Pickup",
                    description = "Collectible power-ups with effects",
                    category = "Power-ups",
                    tags = new[] { "powerup", "pickup", "collectible" },
                    complexity = 2,
                    code = GeneratePowerupCode()
                },
                new CodeTemplate
                {
                    id = "particle_effect",
                    name = "Particle Effect",
                    description = "Trigger particle effects on events",
                    category = "Effects",
                    tags = new[] { "particles", "effects", "visual" },
                    complexity = 1,
                    code = GenerateParticleCode()
                }
            };
        }

        private static string GeneratePlayerControllerCode() => @"using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;

    private Vector2 moveInput;

    private void Update()
    {
        moveInput.x = Input.GetAxisRaw(""Horizontal"");
        moveInput.y = Input.GetAxisRaw(""Vertical"");
        moveInput.Normalize();
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;

        // Face mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}";

        private static string GenerateEnemyAICode() => @"using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private Rigidbody2D rb;
    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag(""Player"")?.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= detectionRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;
        }
    }
}";

        private static string GenerateShootingCode() => @"using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    private float nextFireTime;

    private void Update()
    {
        if (Input.GetButton(""Fire1"") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = firePoint.up * 10f;
    }
}";

        private static string GenerateHealthCode() => @"using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    public UnityEvent<int, int> OnHealthChanged;
    public UnityEvent OnDeath;

    private void Start() => currentHealth = maxHealth;

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0) OnDeath?.Invoke();
    }
}";

        private static string GenerateWaveSpawnerCode() => @"using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int enemiesPerWave = 5;
    private int currentWave = 0;

    private void Start() => StartCoroutine(SpawnWaves());

    private IEnumerator SpawnWaves()
    {
        while (true)
        {
            currentWave++;
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(5f);
        }
    }

    private void SpawnEnemy()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    }
}";

        private static string GenerateHealthBarCode() => @"using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Health playerHealth;

    private void Start()
    {
        playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
    }

    private void UpdateHealthBar(int current, int max)
    {
        fillImage.fillAmount = (float)current / max;
    }
}";

        private static string GenerateCameraFollowCode() => @"using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}";

        private static string GenerateDashCode() => @"using UnityEngine;

public class DashAbility : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool canDash = true;
    private Rigidbody2D rb;

    private void Start() => rb = GetComponent<Rigidbody2D>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            StartCoroutine(Dash());
        }
    }

    private System.Collections.IEnumerator Dash()
    {
        canDash = false;
        Vector2 dashDirection = rb.velocity.normalized;
        rb.velocity = dashDirection * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}";

        private static string GeneratePowerupCode() => @"using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] private float effectDuration = 5f;
    [SerializeField] private float speedMultiplier = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(""Player""))
        {
            ApplyEffect(other.gameObject);
            Destroy(gameObject);
        }
    }

    private void ApplyEffect(GameObject player)
    {
        // Apply powerup effect here
        Debug.Log(""Powerup collected!"");
    }
}";

        private static string GenerateParticleCode() => @"using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    public void PlayEffect()
    {
        if (particles != null) particles.Play();
    }

    public void PlayEffectAtPosition(Vector3 position)
    {
        GameObject effect = Instantiate(gameObject, position, Quaternion.identity);
        effect.GetComponent<ParticleEffect>().PlayEffect();
        Destroy(effect, 2f);
    }
}";
    }
}
