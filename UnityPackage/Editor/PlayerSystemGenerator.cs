using UnityEngine;
using UnityEditor;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Generates player movement and shooting systems
    /// </summary>
    public static class PlayerSystemGenerator
    {
        [MenuItem("Tools/Unity AI Agent/Generate/Player System")]
        public static void GeneratePlayerSystem()
        {
            GeneratePlayerController();
            GeneratePlayerHealth();
            GeneratePlayerShooting();

            EditorUtility.DisplayDialog("Success",
                "Player system generated!\n\nCreated scripts:\n- PlayerController\n- PlayerHealth\n- PlayerShooting",
                "OK");
        }

        public static void GeneratePlayerController()
        {
            string script = @"using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header(""Movement Settings"")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 smoothVelocity;
    private Vector2 currentVelocity;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Get input
        moveInput.x = Input.GetAxisRaw(""Horizontal"");
        moveInput.y = Input.GetAxisRaw(""Vertical"");
        moveInput.Normalize();
    }

    private void FixedUpdate()
    {
        // Move player
        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref currentVelocity, 0.1f);

        // Rotate to face mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}";
            ScriptGeneratorUtility.CreateScript("PlayerController", script);
        }

        public static void GeneratePlayerHealth()
        {
            string script = @"using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header(""Health Settings"")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header(""Events"")]
    public UnityEvent<int, int> OnHealthChanged; // current, max
    public UnityEvent OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Debug.Log(""Player died!"");
        // Add death handling logic here
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)currentHealth / maxHealth;
}";
            ScriptGeneratorUtility.CreateScript("PlayerHealth", script);
        }

        public static void GeneratePlayerShooting()
        {
            string script = @"using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header(""Shooting Settings"")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float projectileSpeed = 10f;

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
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning(""Projectile prefab or fire point not assigned!"");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = firePoint.up * projectileSpeed;
        }
    }
}";
            ScriptGeneratorUtility.CreateScript("PlayerShooting", script);
        }
    }
}
