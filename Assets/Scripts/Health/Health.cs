using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    private const int PlayerLayer = 10;
    private const int EnemyLayer = 11;

    [Header("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    public float CurrentHealth => currentHealth;
    private Animator anim;
    private Rigidbody2D rb;
    private bool dead;
    private bool isPlayerCharacter;
    public bool IsDead => dead;

    private float checkpointHealth;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration;
    [SerializeField] private int numberOfFlashes;
    private SpriteRenderer spriteRend;

    [Header("Components")]
    [SerializeField] private Behaviour[] components;
    private bool invulnerable;
    private int enemyCollisionIgnoreRequests;

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip hurtSound;

    private string originalTag;
    private int originalLayer;

    public event Action<float> OnDamaged;
    public event Action OnDeath;
    public event Action OnRespawned;

    public bool IsInvulnerable => invulnerable;

    private void Awake()
    {
        currentHealth = startingHealth;
        checkpointHealth = currentHealth;
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        isPlayerCharacter = GetComponent<PlayerRespawn>() != null;
        originalTag = gameObject.tag;
        originalLayer = gameObject.layer;
    }

    public void TakeDamage(float _damage)
    {
        if (invulnerable || dead) return;
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);
        OnDamaged?.Invoke(currentHealth);

        if (currentHealth > 0)
        {
            if (OnDamaged == null && anim != null)
                anim.SetTrigger("Hurt");
            StartCoroutine(Invunerability());
            if (hurtSound != null)
                SoundManager.instance.PlaySound(hurtSound);
        }
        else
        {
            if (!dead)
            {
                dead = true;
                StopAllCoroutines();
                enemyCollisionIgnoreRequests = 0;
                ApplyPlayerEnemyCollisionIgnore();
                invulnerable = false;
                if (isPlayerCharacter)
                {
                    gameObject.tag = "Untagged";
                    gameObject.layer = 0;
                }

                if (OnDeath != null)
                {
                    OnDeath.Invoke();
                }
                else if (anim != null)
                {
                    anim.SetBool("Grounded", true);
                    anim.SetTrigger("Die");
                }

                foreach (Behaviour component in components)
                    if (ShouldDisableOnDeath(component))
                        component.enabled = false;

                if (deathSound != null)
                    SoundManager.instance.PlaySound(deathSound);
            }
        }
    }

    public void AddHealth(float _value)
    {
        currentHealth = Mathf.Clamp(currentHealth + _value, 0, startingHealth);
    }

    private IEnumerator Invunerability()
    {
        invulnerable = true;
        SetEnemyCollisionIgnored(true);
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        SetEnemyCollisionIgnored(false);
        invulnerable = false;
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void SetCheckpointHealth()
    {
        checkpointHealth = currentHealth;
    }

    public void Respawn()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        StopAllCoroutines();
        dead = false;
        invulnerable = false;
        currentHealth = checkpointHealth;
        if (isPlayerCharacter)
        {
            gameObject.tag = originalTag;
            gameObject.layer = originalLayer;
        }
        enemyCollisionIgnoreRequests = 0;
        ApplyPlayerEnemyCollisionIgnore();
        if (spriteRend != null)
            spriteRend.color = Color.white;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (OnRespawned != null)
        {
            OnRespawned.Invoke();
        }
        else if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.Play("Idle");
        }
        StartCoroutine(Invunerability());

        foreach (Behaviour component in components)
            if (component != null)
                component.enabled = true;
    }

    private bool ShouldDisableOnDeath(Behaviour component)
    {
        return component != null
               && component != this
               && component != anim
               && component is not PlayerRespawn;
    }

    public bool Invulnerable => invulnerable;

    public void SetEnemyCollisionIgnored(bool ignored)
    {
        enemyCollisionIgnoreRequests += ignored ? 1 : -1;
        if (enemyCollisionIgnoreRequests < 0)
            enemyCollisionIgnoreRequests = 0;

        ApplyPlayerEnemyCollisionIgnore();
    }

    private void ApplyPlayerEnemyCollisionIgnore()
    {
        Physics2D.IgnoreLayerCollision(PlayerLayer, EnemyLayer, enemyCollisionIgnoreRequests > 0);
    }

}
