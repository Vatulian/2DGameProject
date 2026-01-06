using UnityEngine;

public class EnemyHitReaction : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private HitFlash hitFlash;
    [SerializeField] private FlameEnemyAI flameAI;

    [Header("Hit Control")]
    [SerializeField] private float hitCooldown = 0.2f; // spam engeli

    private float lastHealth;
    private float nextAllowedTime;
    private bool dead;

    private void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!hitFlash) hitFlash = GetComponent<HitFlash>();
        if (!flameAI) flameAI = GetComponent<FlameEnemyAI>();

        if (health != null)
            lastHealth = health.currentHealth;
    }

    private void Update()
    {
        if (health == null || dead) return;

        // Death yakala (hit flash yok)
        if (health.currentHealth <= 0f)
        {
            dead = true;
            return;
        }

        // Hit aldı mı?
        if (health.currentHealth < lastHealth)
        {
            if (Time.time >= nextAllowedTime)
            {
                nextAllowedTime = Time.time + hitCooldown;

                // 🔥 Görsel feedback
                if (hitFlash != null)
                    hitFlash.Play();

                // 🔥 Atak iptali
                if (flameAI != null)
                    flameAI.InterruptForHit();
            }
        }

        lastHealth = health.currentHealth;
    }
}