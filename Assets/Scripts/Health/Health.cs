using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    private const int PlayerLayer = 10;
    private const int EnemyLayer = 11;

    [Header("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    private PlayerAnimationController playerAnimationController;
    private PlayerMeleeAttack playerMeleeAttack;
    private PlayerRespawn playerRespawn;
    private Rigidbody2D rb;
    private bool dead;
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

    public bool IsInvulnerable => invulnerable;

    private void Awake()
    {
        currentHealth = startingHealth;
        checkpointHealth = currentHealth;
        anim = GetComponent<Animator>();
        playerAnimationController = GetComponent<PlayerAnimationController>();
        playerMeleeAttack = GetComponent<PlayerMeleeAttack>();
        playerRespawn = GetComponent<PlayerRespawn>();
        spriteRend = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalTag = gameObject.tag;
        originalLayer = gameObject.layer;
    }

    public void TakeDamage(float _damage)
    {
        if (playerAnimationController == null)
            playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerMeleeAttack == null)
            playerMeleeAttack = GetComponent<PlayerMeleeAttack>();

        if (invulnerable || dead) return;
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            playerMeleeAttack?.ResetCombo();
            if (playerAnimationController != null) playerAnimationController.PlayHurt();
            else if (anim != null) anim.SetTrigger("Hurt");
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
                playerMeleeAttack?.ResetCombo(false);
                if (playerRespawn != null)
                {
                    gameObject.tag = "Untagged";
                    gameObject.layer = 0;
                }

                if (playerAnimationController != null) playerAnimationController.PlayDeath();
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
        if (playerAnimationController == null)
            playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerMeleeAttack == null)
            playerMeleeAttack = GetComponent<PlayerMeleeAttack>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        StopAllCoroutines();
        dead = false;
        invulnerable = false;
        currentHealth = checkpointHealth;
        if (playerRespawn != null)
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
        playerMeleeAttack?.ResetCombo(false);
        if (playerAnimationController != null) playerAnimationController.PlayRespawn();
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
               && component != playerAnimationController
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
