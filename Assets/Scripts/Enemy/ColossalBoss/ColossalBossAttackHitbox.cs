using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ColossalBossAttackHitbox : MonoBehaviour
{
    private const string EnemyAttackLayerName = "EnemyAttack";

    [Header("Refs")]
    [SerializeField] private BoxCollider2D attackCollider;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool applyKnockback = true;

    private readonly HashSet<Collider2D> hitColliders = new HashSet<Collider2D>();
    private readonly HashSet<Health> hitHealthTargets = new HashSet<Health>();

    private Transform owner;
    private Vector3 rightFacingLocalPosition;
    private Vector2 rightFacingColliderOffset;
    private bool cachedDefaults;

    protected int Facing { get; private set; } = 1;
    protected bool IsActive { get; private set; }
    protected BoxCollider2D AttackCollider => attackCollider;
    protected Coroutine TimedRoutine { get; private set; }

    protected virtual void Awake()
    {
        ApplyEnemyAttackLayer();
        CacheDefaults();

        if (attackCollider != null)
        {
            attackCollider.isTrigger = true;
            attackCollider.enabled = false;
        }
    }

    public virtual void Begin(Transform attackOwner, int attackFacing)
    {
        owner = attackOwner;
        hitColliders.Clear();
        hitHealthTargets.Clear();
        SetFacing(attackFacing);
        DisableHitbox();
    }

    public virtual void SetFacing(int attackFacing)
    {
        CacheDefaults();
        Facing = attackFacing >= 0 ? 1 : -1;
        ResetLocalPosition();

        if (attackCollider != null)
            attackCollider.offset = new Vector2(rightFacingColliderOffset.x * Facing, rightFacingColliderOffset.y);
    }

    public virtual void EnableTimed(float duration)
    {
        StopTimedRoutine();
        SetActive(true);

        if (duration > 0f)
            TimedRoutine = StartCoroutine(DisableAfter(duration));
    }

    public virtual void EnableHitbox()
    {
        StopTimedRoutine();
        SetActive(true);
    }

    public virtual void DisableHitbox()
    {
        StopTimedRoutine();
        SetActive(false);
        ResetLocalPosition();
    }

    public Vector3 GetWorldCenter()
    {
        if (attackCollider == null)
            return transform.position;

        return transform.TransformPoint(attackCollider.offset);
    }

    public virtual void MoveToImpactPosition()
    {
    }

    protected virtual void ResetLocalPosition()
    {
        ApplyFacingToLocalPosition(rightFacingLocalPosition);
    }

    protected void SetActive(bool value)
    {
        IsActive = value;

        if (attackCollider != null)
            attackCollider.enabled = value;
    }

    protected void SetTimedRoutine(IEnumerator routine)
    {
        TimedRoutine = StartCoroutine(routine);
    }

    protected void StopTimedRoutine()
    {
        if (TimedRoutine == null)
            return;

        StopCoroutine(TimedRoutine);
        TimedRoutine = null;
    }

    protected void ClearTimedRoutine()
    {
        TimedRoutine = null;
    }

    protected void ApplyFacingToLocalPosition(Vector3 rightFacingPosition)
    {
        Vector3 position = rightFacingPosition;
        position.x = Mathf.Abs(position.x) * Facing;
        transform.localPosition = position;
    }

    protected Vector3 GetColliderWorldSize()
    {
        Vector2 size = attackCollider != null ? attackCollider.size : Vector2.one;
        Vector3 scale = transform.lossyScale;
        return new Vector3(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), 0.01f);
    }

    protected Vector2 GetColliderOffset()
    {
        return attackCollider != null ? attackCollider.offset : Vector2.zero;
    }

    private IEnumerator DisableAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        ClearTimedRoutine();
        SetActive(false);
    }

    private void CacheDefaults()
    {
        if (cachedDefaults)
            return;

        if (attackCollider == null)
            attackCollider = GetComponent<BoxCollider2D>();

        rightFacingLocalPosition = transform.localPosition;
        rightFacingColliderOffset = attackCollider != null ? attackCollider.offset : Vector2.zero;
        cachedDefaults = true;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!IsActive || other == null || hitColliders.Contains(other))
            return;

        if (!CanTarget(other))
            return;

        Health targetHealth = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (targetHealth == null || hitHealthTargets.Contains(targetHealth))
            return;

        hitColliders.Add(other);
        hitHealthTargets.Add(targetHealth);

        if (applyKnockback)
            targetHealth.TakeDamage(damage, owner != null ? owner.position : transform.position);
        else
            targetHealth.TakeDamage(damage);
    }

    private bool CanTarget(Collider2D other)
    {
        if (playerLayer.value != 0 && (playerLayer.value & (1 << other.gameObject.layer)) == 0)
            return false;

        return other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
    }

    private void ApplyEnemyAttackLayer()
    {
        int enemyAttackLayer = LayerMask.NameToLayer(EnemyAttackLayerName);
        if (enemyAttackLayer >= 0 && gameObject.layer != enemyAttackLayer)
            gameObject.layer = enemyAttackLayer;
    }

    protected virtual void OnValidate()
    {
        ApplyEnemyAttackLayer();

        if (attackCollider == null)
            attackCollider = GetComponent<BoxCollider2D>();

        if (attackCollider != null)
            attackCollider.isTrigger = true;
    }
}
