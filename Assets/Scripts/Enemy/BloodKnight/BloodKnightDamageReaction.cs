using System.Collections;
using UnityEngine;

public class BloodKnightDamageReaction : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private Animator anim;
    [SerializeField] private BloodKnightAI ai;
    [SerializeField] private BloodKnightAttack attack;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer[] renderers;

    [Header("Animation States")]
    [SerializeField] private string hitStateName = "hit";
    [SerializeField] private string deathStateName = "death or teleport";
    [SerializeField, Range(0f, 0.12f)] private float animationTransitionTime = 0.03f;

    [Header("Feedback")]
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionTime = 0.35f;
    [SerializeField] private float knockbackDistance = 0.72f;
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Death")]
    [SerializeField] private bool disableBodyColliderOnDeath = true;
    [SerializeField] private bool useDeathAnimationEvent = true;
    [SerializeField] private float fallbackDeactivateDelay = 2f;

    private Color[] originalColors;
    private Coroutine flashRoutine;
    private Coroutine knockbackRoutine;
    private Coroutine deathFallbackRoutine;
    private bool dead;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (ai == null)
            ai = GetComponent<BloodKnightAI>();

        if (attack == null)
            attack = GetComponentInChildren<BloodKnightAttack>(true);

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    private void HandleDamaged(float remainingHealth)
    {
        if (remainingHealth <= 0f)
            return;

        PlayState(hitStateName);
        ai?.InterruptForHit(hitReactionTime);
        attack?.DisableHitbox();

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(Flash());

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockBackFromPlayer());
    }

    private void HandleDeath()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        dead = true;
        RestoreColors();
        attack?.DisableHitbox();

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (ai != null)
            ai.enabled = false;

        if (attack != null)
            attack.enabled = false;

        if (bodyCollider != null && disableBodyColliderOnDeath)
            bodyCollider.enabled = false;

        PlayState(deathStateName);

        if (deathFallbackRoutine != null)
            StopCoroutine(deathFallbackRoutine);

        deathFallbackRoutine = StartCoroutine(DeactivateAfterFallbackDelay());
    }

    private IEnumerator Flash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);
        RestoreColors();
        flashRoutine = null;
    }

    private IEnumerator KnockBackFromPlayer()
    {
        if (knockbackDistance <= 0f || hitReactionTime <= 0f)
        {
            knockbackRoutine = null;
            yield break;
        }

        Transform attacker = PlayerReference.Player;
        float direction = GetKnockbackDirection(attacker);
        Vector3 start = transform.position;
        Vector3 target = start + Vector3.right * direction * knockbackDistance;

        if (ai != null)
            target = ai.ClampPositionToPatrolBounds(target);

        float elapsed = 0f;
        while (elapsed < hitReactionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hitReactionTime);
            float eased = knockbackCurve != null ? knockbackCurve.Evaluate(t) : t;
            transform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        transform.position = target;
        knockbackRoutine = null;
    }

    private float GetKnockbackDirection(Transform attacker)
    {
        if (attacker != null)
            return attacker.position.x <= transform.position.x ? 1f : -1f;

        if (ai != null)
            return -ai.Facing;

        return transform.localScale.x >= 0f ? -1f : 1f;
    }

    private void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originalColors[i];
        }
    }

    public void DeactivateSelf()
    {
        if (!dead)
            return;

        if (deathFallbackRoutine != null)
        {
            StopCoroutine(deathFallbackRoutine);
            deathFallbackRoutine = null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator DeactivateAfterFallbackDelay()
    {
        if (useDeathAnimationEvent && fallbackDeactivateDelay <= 0f)
            yield break;

        yield return new WaitForSeconds(fallbackDeactivateDelay);
        DeactivateSelf();
    }

    private void PlayState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || anim == null || anim.runtimeAnimatorController == null)
            return;

        anim.CrossFadeInFixedTime(stateName, animationTransitionTime);
    }
}
