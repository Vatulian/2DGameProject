using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    [Header("Stats")]
    public int health = 20;
    public int damage = 1;

    [SerializeField] private float damageCooldown = 1.5f;
    private float timeBtwDamage;

    [Header("UI")]
    [SerializeField] private Slider healthBar;

    [Header("Components")]
    private Animator anim;
    private HitFlash hitFlash;          // beyaz flick
    private BossHitVFX hitVfx;          // patlama/shake (opsiyonel)

    public bool isDead;
    private bool stageTwoTriggered;

    private void Start()
    {
        anim = GetComponent<Animator>();
        hitFlash = GetComponent<HitFlash>();
        hitVfx = GetComponent<BossHitVFX>();

        timeBtwDamage = 0f;

        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (timeBtwDamage > 0f)
            timeBtwDamage -= Time.deltaTime;

        if (healthBar != null)
            healthBar.value = health;

        if (!stageTwoTriggered && health <= 10)
        {
            stageTwoTriggered = true;
            anim.SetTrigger("stageTwo");
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (!other.CompareTag("Player")) return;
        if (timeBtwDamage > 0f) return;

        var hp = other.GetComponent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
            timeBtwDamage = damageCooldown;
        }
    }

    // Konumsuz hasar (gereken yerlerde kolay çağrılsın diye)
    public void TakeDamage(int amount)
    {
        TakeDamageAt(amount, transform.position);
    }

    // Vuruş noktasıyla hasar (Projectile burayı çağırır)
    public void TakeDamageAt(int amount, Vector3 hitWorldPos)
    {
        if (isDead) return;

        health = Mathf.Max(0, health - amount);

        if (healthBar != null)
            healthBar.value = health;

        // Görsel/ses geri bildirimleri
        hitFlash?.Play();               // beyaz flick
        hitVfx?.PlayAt(hitWorldPos);    // patlama + shake + sfx (varsa)

        if (!stageTwoTriggered && health <= 10)
        {
            stageTwoTriggered = true;
            anim.SetTrigger("stageTwo");
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Son beyaz yanıp sönme efekti (opsiyonel)
        GetComponent<HitFlash>()?.Play();

        anim.SetTrigger("death");

        // Can barını gizle
        if (healthBar != null)
            healthBar.gameObject.SetActive(false);
    }

}
