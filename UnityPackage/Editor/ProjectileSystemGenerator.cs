using UnityEngine;
using UnityEditor;

namespace SparkGames.UnityAIAgent.Editor
{
    /// <summary>
    /// Generates projectile and combat systems
    /// </summary>
    public static class ProjectileSystemGenerator
    {
        [MenuItem("Tools/Unity AI Agent/Generate/Projectile System")]
        public static void GenerateProjectileSystem()
        {
            GenerateProjectile();
            GenerateDamageDealer();

            EditorUtility.DisplayDialog("Success",
                "Projectile system generated!\n\nCreated scripts:\n- Projectile\n- DamageDealer",
                "OK");
        }

        public static void GenerateProjectile()
        {
            string script = @"using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header(""Projectile Settings"")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyOnHit = true;

    [Header(""Tags to Hit"")]
    [SerializeField] private string[] tagsToHit = { ""Enemy"" };

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.velocity = transform.up * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        foreach (string tag in tagsToHit)
        {
            if (other.CompareTag(tag))
            {
                // Deal damage
                EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                }

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
                break;
            }
        }
    }
}";
            ScriptGeneratorUtility.CreateScript("Projectile", script);
        }

        public static void GenerateDamageDealer()
        {
            string script = @"using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private string targetTag = ""Player"";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
}";
            ScriptGeneratorUtility.CreateScript("DamageDealer", script);
        }
    }
}
