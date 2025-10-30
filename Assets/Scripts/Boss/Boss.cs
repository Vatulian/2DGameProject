using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    public int health;
    public int damage;
    [SerializeField] private float damageCooldown = 1.5f;
    private float timeBtwDamage;

    public Slider healthBar;
    private Animator anim;
    public bool isDead;

    private void Start()
    {
        anim = GetComponent<Animator>();
        timeBtwDamage = 0f;

        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
        }
    }

    private void Update()
    {
        if (health <= 25)
            anim.SetTrigger("stageTwo");

        if (health <= 0)
            anim.SetTrigger("death");

        if (timeBtwDamage > 0f)
            timeBtwDamage -= Time.deltaTime;

        if (healthBar != null)
            healthBar.value = health;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (!other.CompareTag("Player")) return;
        if (timeBtwDamage > 0f) return;

        var hp = other.GetComponent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(damage);      // Mevcut sistemin tüm efektleri burada çalışır
            timeBtwDamage = damageCooldown;
        }
    }

    // (opsiyonel) Boss’un kendisi hasar alacaksa:
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        health = Mathf.Max(0, health - amount);
        if (healthBar != null) healthBar.value = health;
        if (health <= 0) anim.SetTrigger("death");
        else anim.SetTrigger("Hit"); // varsa
    }
    
}