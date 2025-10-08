using UnityEngine;
using UnityEditor;

namespace SparkGames.UnityAIAgent.Editor
{
    /// <summary>
    /// Generates enemy AI and behavior systems
    /// </summary>
    public static class EnemySystemGenerator
    {
        [MenuItem("Tools/Unity AI Agent/Generate/Enemy System")]
        public static void GenerateEnemySystem()
        {
            GenerateEnemyAI();
            GenerateEnemyHealth();
            GenerateEnemyAttack();

            EditorUtility.DisplayDialog("Success",
                "Enemy system generated!\n\nCreated scripts:\n- EnemyAI\n- EnemyHealth\n- EnemyAttack",
                "OK");
        }

        public static void GenerateEnemyAI()
        {
            string script = @"using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header(""AI Settings"")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float stopDistance = 1f;

    [Header(""References"")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;

    private Vector2 targetVelocity;
    private bool isAttacking;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(""Player"");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer > stopDistance)
            {
                // Move toward player
                Vector2 direction = (player.position - transform.position).normalized;
                targetVelocity = direction * moveSpeed;
                isAttacking = false;
            }
            else
            {
                // Stop and attack
                targetVelocity = Vector2.zero;
                isAttacking = distanceToPlayer <= attackRange;
            }

            // Face player
            Vector2 lookDir = player.position - transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            targetVelocity = Vector2.zero;
            isAttacking = false;
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, 0.1f);
    }

    public bool IsAttacking() => isAttacking;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}";
            ScriptGeneratorUtility.CreateScript("EnemyAI", script);
        }

        public static void GenerateEnemyHealth()
        {
            string script = @"using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header(""Health Settings"")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int currentHealth;

    [Header(""Events"")]
    public UnityEvent OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}";
            ScriptGeneratorUtility.CreateScript("EnemyHealth", script);
        }

        public static void GenerateEnemyAttack()
        {
            string script = @"using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header(""Attack Settings"")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1f;

    private float lastAttackTime;
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    private void Update()
    {
        if (enemyAI != null && enemyAI.IsAttacking() && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    private void Attack()
    {
        GameObject player = GameObject.FindGameObjectWithTag(""Player"");
        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($""Enemy dealt {damage} damage to player"");
            }
        }
    }
}";
            ScriptGeneratorUtility.CreateScript("EnemyAttack", script);
        }
    }
}
