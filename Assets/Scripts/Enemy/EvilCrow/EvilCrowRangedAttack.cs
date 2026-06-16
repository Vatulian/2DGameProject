using System;
using UnityEngine;

public class EvilCrowRangedAttack : MonoBehaviour
{
    private static readonly int AttackStateHash = Animator.StringToHash("Attack Start");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform firePoint;
    [SerializeField] private EvilCrowProjectile projectilePrefab;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float cooldown = 1.5f;
    [SerializeField, Min(0.1f)] private float animationEventTimeout = 0.75f;
    [SerializeField] private bool trackTargetUntilFire = true;

    [Header("Spawn Tuning")]
    [SerializeField] private Vector2 projectileSpawnOffset;
    [SerializeField] private bool drawSpawnGizmo = true;

    public event Action AttackFinished;

    public bool IsAttacking { get; private set; }
    public bool IsReady => !IsAttacking && cooldownTimer <= 0f;

    private Transform target;
    private Vector2 lockedAimPoint;
    private float cooldownTimer;
    private float attackTimeoutTimer;
    private bool projectileFired;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (firePoint == null)
            firePoint = transform;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!IsAttacking)
            return;

        attackTimeoutTimer -= Time.deltaTime;
        if (attackTimeoutTimer <= 0f)
            FinishAttackFromAnimation();
    }

    public bool TryStartAttack(Transform newTarget)
    {
        if (!IsReady || newTarget == null || projectilePrefab == null)
            return false;

        target = newTarget;
        lockedAimPoint = newTarget.position;
        projectileFired = false;
        IsAttacking = true;
        attackTimeoutTimer = animationEventTimeout;

        if (animator != null)
            animator.Play(AttackStateHash, 0, 0f);

        return true;
    }

    public void FireProjectileFromAnimation()
    {
        if (!IsAttacking || projectileFired || projectilePrefab == null)
            return;

        projectileFired = true;
        Vector2 aimPoint = trackTargetUntilFire && target != null
            ? target.position
            : lockedAimPoint;

        Vector2 origin = GetProjectileOrigin();
        Vector2 direction = aimPoint - origin;
        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.right;

        EvilCrowProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Initialize(direction.normalized, gameObject);
    }

    private Vector2 GetProjectileOrigin()
    {
        Transform originTransform = firePoint != null ? firePoint : transform;
        return (Vector2)originTransform.position + (Vector2)originTransform.TransformVector(projectileSpawnOffset);
    }

    public void FinishAttackFromAnimation()
    {
        if (!IsAttacking)
            return;

        IsAttacking = false;
        target = null;
        cooldownTimer = cooldown;
        AttackFinished?.Invoke();
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        target = null;
        projectileFired = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnGizmo)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetProjectileOrigin(), 0.08f);
    }
}
