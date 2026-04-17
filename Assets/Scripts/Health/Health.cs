using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    private PlayerAnimationController playerAnimationController;
    private PlayerMeleeAttack playerMeleeAttack;
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

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip hurtSound;

    
    public bool IsInvulnerable => invulnerable;

    private void Awake()
    {
        currentHealth = startingHealth;
        checkpointHealth = currentHealth;
        anim = GetComponent<Animator>();
        playerAnimationController = GetComponent<PlayerAnimationController>();
        playerMeleeAttack = GetComponent<PlayerMeleeAttack>();
        spriteRend = GetComponent<SpriteRenderer>();
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
                Physics2D.IgnoreLayerCollision(10, 11, false);
                invulnerable = false;
                playerMeleeAttack?.ResetCombo(false);

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
        Physics2D.IgnoreLayerCollision(10, 11, true);
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(10, 11, false);
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

        dead = false;
        currentHealth = checkpointHealth;
        Physics2D.IgnoreLayerCollision(10, 11, false);
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

}
