using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMana))]
public class PlayerSpecialMove : MonoBehaviour
{
    private const int FxPoolSize = 3;
    private const float SwordPixelsPerUnit = 16f;

    [Header("Input")]
    [SerializeField] private KeyCode specialMoveKey = KeyCode.Mouse1;

    [Header("Mana and Timing")]
    [SerializeField, Min(0)] private int manaCost = 30;
    [SerializeField, Min(0f)] private float cooldown = 1.5f;
    [SerializeField, Min(0.1f)] private float castDuration = 0.62f;
    [SerializeField, Min(0f)] private float swordRepeatDelay = 0.18f;

    [Header("Player Animation")]
    [SerializeField] private string slamStateName = "Slam with no FX";
    [SerializeField, Range(0f, 0.12f)] private float animationTransitionDuration = 0.02f;

    [Header("FX")]
    [SerializeField] private GameObject fxPrefab;
    [SerializeField] private string sliceStateName = "Dark Slice from Up";
    [SerializeField] private string swordStateName = "Dark Swords";
    [SerializeField] private float sliceFxDuration = 0.52f;
    [SerializeField] private float swordFxDuration = 1.02f;
    [SerializeField] private Vector2 sliceOffset = new Vector2(0.9f, -0.25f);
    [SerializeField] private float firstSwordDistance = 3f;
    [SerializeField] private float secondSwordDistance = 7.8f;
    [SerializeField] private float swordVerticalOffset = -0.15f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int sliceDamage = 2;
    [SerializeField] private Vector2 sliceHitboxSize = new Vector2(3f, 2.2f);
    [SerializeField, Min(0)] private int swordDamage = 2;
    [SerializeField] private Vector2 swordPixelSize = new Vector2(85f, 44f);
    [SerializeField, Min(0f)] private float swordHitboxExtraHeight = 0.3f;
    [SerializeField] private LayerMask enemyLayers = 1 << 11;

    [Header("Enemy Knockback")]
    [SerializeField, Min(0f)] private float knockbackDistance = 1.05f;
    [SerializeField, Min(0f)] private float knockbackUpwardDistance = 0.12f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.16f;

    private readonly GameObject[] fxPool = new GameObject[FxPoolSize];
    private PlayerMana playerMana;
    private PlayerMovement playerMovement;
    private PlayerAnimationController animationController;
    private PlayerMeleeAttack meleeAttack;
    private PlayerAttack rangedAttack;
    private PlayerParry parry;
    private Health health;
    private Coroutine moveRoutine;
    private float cooldownTimer;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        playerMana = GetComponent<PlayerMana>();
        playerMovement = GetComponent<PlayerMovement>();
        animationController = GetComponentInChildren<PlayerAnimationController>();
        meleeAttack = GetComponent<PlayerMeleeAttack>();
        rangedAttack = GetComponent<PlayerAttack>();
        parry = GetComponent<PlayerParry>();
        health = GetComponent<Health>();
        PrepareFxPool();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;

        CancelMove(false);
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (health != null && health.IsDead)
            return;

        if (Input.GetKeyDown(specialMoveKey))
            TryStartMove();
    }

    public bool TryStartMove()
    {
        if (!CanStartMove())
            return false;

        int effectiveManaCost = playerMana != null ? playerMana.GetModifiedManaCost(manaCost) : manaCost;
        if (effectiveManaCost > 0 && (playerMana == null || !playerMana.TrySpendMana(effectiveManaCost)))
            return false;

        cooldownTimer = cooldown;
        moveRoutine = StartCoroutine(PerformMove());
        return true;
    }

    private bool CanStartMove()
    {
        if (IsActive || cooldownTimer > 0f || playerMovement == null || !playerMovement.canAttack())
            return false;

        if (!playerMovement.IsGrounded())
            return false;

        if (meleeAttack != null && meleeAttack.IsAttacking)
            return false;

        if (rangedAttack != null && rangedAttack.IsAttacking)
            return false;

        return parry == null || !parry.IsParryActive;
    }

    private IEnumerator PerformMove()
    {
        IsActive = true;
        float facing = playerMovement != null && !playerMovement.IsFacingRight ? -1f : 1f;
        float startedAt = Time.time;

        playerMovement?.SetExternalRunMultiplier(0f);
        playerMovement?.LockHorizontalMovement(castDuration);
        animationController?.PlayLockedState(slamStateName, animationTransitionDuration, castDuration);

        Vector2 sliceCenter = GetOffsetPosition(sliceOffset, facing);
        PlayFx(0, sliceStateName, sliceCenter, facing, sliceFxDuration, false);

        Vector2 firstSwordCenter = GetForwardPosition(firstSwordDistance, swordVerticalOffset, facing);
        PlayFx(1, swordStateName, firstSwordCenter, facing, swordFxDuration, true);

        yield return new WaitForSeconds(swordRepeatDelay);

        Vector2 secondSwordCenter = GetForwardPosition(secondSwordDistance, swordVerticalOffset, facing);
        PlayFx(2, swordStateName, secondSwordCenter, facing, swordFxDuration, true);

        float remainingCastTime = castDuration - (Time.time - startedAt);
        if (remainingCastTime > 0f)
            yield return new WaitForSeconds(remainingCastTime);

        FinishMove();
    }

    private void FinishMove()
    {
        IsActive = false;
        moveRoutine = null;
        playerMovement?.ResetExternalRunMultiplier();
        animationController?.ReturnToLocomotionState();
    }

    private void CancelMove(bool returnToLocomotion)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        IsActive = false;
        playerMovement?.ResetExternalRunMultiplier();

        for (int i = 0; i < fxPool.Length; i++)
        {
            if (fxPool[i] != null)
                fxPool[i].SetActive(false);
        }

        if (returnToLocomotion)
            animationController?.ReturnToLocomotionState();
    }

    private void HandleDamaged(float remainingHp)
    {
        CancelMove(false);
    }

    private void PrepareFxPool()
    {
        if (fxPrefab == null)
        {
            Debug.LogError("PlayerSpecialMove requires an FX prefab.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < fxPool.Length; i++)
        {
            GameObject fxObject = Instantiate(fxPrefab);
            fxObject.name = i == 0 ? fxPrefab.name : fxPrefab.name + "_" + i;
            fxObject.SetActive(false);
            fxPool[i] = fxObject;
        }
    }

    private void PlayFx(
        int poolIndex,
        string stateName,
        Vector2 position,
        float facing,
        float duration,
        bool useSwordHitbox)
    {
        if (poolIndex < 0 || poolIndex >= fxPool.Length || fxPool[poolIndex] == null)
            return;

        GameObject fxObject = fxPool[poolIndex];
        fxObject.transform.position = new Vector3(position.x, position.y, transform.position.z);
        fxObject.SetActive(true);

        SpriteRenderer renderer = fxObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.flipX = facing < 0f;

        PlayerSpecialMoveFxEventForwarder eventForwarder =
            fxObject.GetComponent<PlayerSpecialMoveFxEventForwarder>();
        eventForwarder?.Begin(this, useSwordHitbox);

        Animator animator = fxObject.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        StartCoroutine(HideFxAfter(fxObject, duration));
    }

    private static IEnumerator HideFxAfter(GameObject fxObject, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (fxObject != null)
            fxObject.SetActive(false);
    }

    private Vector2 GetOffsetPosition(Vector2 offset, float facing)
    {
        return (Vector2)transform.position + new Vector2(offset.x * facing, offset.y);
    }

    private Vector2 GetForwardPosition(float distance, float verticalOffset, float facing)
    {
        return (Vector2)transform.position + new Vector2(distance * facing, verticalOffset);
    }

    private Vector2 GetSwordWorldSize()
    {
        Vector2 size = swordPixelSize / SwordPixelsPerUnit;
        size.y += swordHitboxExtraHeight;
        return size;
    }

    public void ApplyFxDamage(Vector2 center, bool useSwordHitbox)
    {
        Vector2 size = useSwordHitbox ? GetSwordWorldSize() : sliceHitboxSize;
        int damage = useSwordHitbox ? swordDamage : sliceDamage;
        DamageArea(center, size, damage);
    }

    private void DamageArea(Vector2 center, Vector2 size, int damage)
    {
        if (damage <= 0)
            return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayers);
        HashSet<Health> damagedHealth = new HashSet<Health>();
        HashSet<Boss> damagedBosses = new HashSet<Boss>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            Health targetHealth = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
            if (targetHealth != null && targetHealth.gameObject != gameObject && damagedHealth.Add(targetHealth))
            {
                targetHealth.TakeDamageAt(damage, hit.bounds.ClosestPoint(center));
                ApplyEnemyKnockback(targetHealth);
                if (targetHealth.GetComponentInParent<BossDamageFeedback>() == null
                    && targetHealth.GetComponentInChildren<BossDamageFeedback>() == null)
                {
                    targetHealth.GetComponentInChildren<HitFlash>()?.Play();
                }
                continue;
            }

            Boss boss = hit.GetComponent<Boss>() ?? hit.GetComponentInParent<Boss>();
            if (boss != null && damagedBosses.Add(boss))
                boss.TakeDamageAt(damage, hit.bounds.ClosestPoint(center));
        }
    }

    private void ApplyEnemyKnockback(Health targetHealth)
    {
        if (targetHealth == null || IsBossTarget(targetHealth))
            return;

        // BloodKnight already performs its own hit-reaction knockback for every damage event.
        if (targetHealth.GetComponentInParent<BloodKnightDamageReaction>() != null)
            return;

        SpecialMoveKnockbackReceiver receiver =
            targetHealth.GetComponent<SpecialMoveKnockbackReceiver>();
        if (receiver == null)
            receiver = targetHealth.gameObject.AddComponent<SpecialMoveKnockbackReceiver>();

        receiver.Apply(transform.position, knockbackDistance, knockbackUpwardDistance, knockbackDuration);
    }

    private static bool IsBossTarget(Health targetHealth)
    {
        if (targetHealth.GetComponentInParent<Boss>() != null)
            return true;

        MonoBehaviour[] behaviours = targetHealth.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IBossEncounterTarget)
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        float facing = playerMovement != null && !playerMovement.IsFacingRight ? -1f : 1f;

        Gizmos.color = new Color(0.65f, 0.2f, 1f, 0.8f);
        Gizmos.DrawWireCube(GetOffsetPosition(sliceOffset, facing), sliceHitboxSize);

        Gizmos.color = new Color(0.25f, 0.55f, 1f, 0.8f);
        Vector2 swordSize = GetSwordWorldSize();
        Gizmos.DrawWireCube(GetForwardPosition(firstSwordDistance, swordVerticalOffset, facing), swordSize);
        Gizmos.DrawWireCube(GetForwardPosition(secondSwordDistance, swordVerticalOffset, facing), swordSize);
    }
}
