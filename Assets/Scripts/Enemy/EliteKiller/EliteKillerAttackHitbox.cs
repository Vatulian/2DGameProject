using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EliteKillerAttackHitbox : MonoBehaviour
{
    private const string EnemyAttackLayerName = "EnemyAttack";

    private enum HitboxRole
    {
        Melee,
        Chain
    }

    [Header("Refs")]
    [SerializeField] private BoxCollider2D attackCollider;

    [Header("Role")]
    [SerializeField] private HitboxRole role = HitboxRole.Melee;

    [Header("Default Melee Hitbox")]
    [SerializeField] private Vector2 meleeSize = new Vector2(1.25f, 1f);
    [SerializeField] private Vector2 meleeRightFacingOffset = new Vector2(0.85f, 0f);

    [Header("Chain Hitbox")]
    [SerializeField] private Vector2 chainStartSize = new Vector2(0.2f, 0.75f);
    [SerializeField] private Vector2 chainStartRightFacingOffset = new Vector2(0.35f, 0f);
    [SerializeField] private Vector2 chainExtendedSize = new Vector2(3.2f, 0.75f);
    [SerializeField] private Vector2 chainExtendedRightFacingOffset = new Vector2(1.85f, 0f);
    [SerializeField] private AnimationCurve chainExtendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve chainRetractCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Chain Preview")]
    [SerializeField] private bool previewChainShape;
    [SerializeField, Range(0f, 1f)] private float chainPreviewProgress = 1f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool applyKnockback = true;

    [Header("Parry")]
    [SerializeField] private ParryAttackSettings parry = new ParryAttackSettings();

    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private readonly HashSet<Health> hitHealthTargets = new HashSet<Health>();

    private Transform owner;
    private int facing = 1;
    private bool active;
    private Coroutine timedWindowRoutine;
    private Coroutine chainRoutine;

    public float MeleeForwardInnerReach => Mathf.Max(0f, Mathf.Abs(meleeRightFacingOffset.x) - meleeSize.x * 0.5f);
    public float MeleeForwardCenter => Mathf.Abs(meleeRightFacingOffset.x);
    public float MeleeForwardReach => Mathf.Abs(meleeRightFacingOffset.x) + meleeSize.x * 0.5f;
    public float ChainForwardReach => Mathf.Abs(chainExtendedRightFacingOffset.x) + chainExtendedSize.x * 0.5f;

    private void Awake()
    {
        ApplyEnemyAttackLayer();

        if (attackCollider == null)
            attackCollider = GetComponent<BoxCollider2D>();

        if (attackCollider == null)
            return;

        attackCollider.isTrigger = true;
        attackCollider.enabled = false;
        ApplyShape(meleeSize, meleeRightFacingOffset);
    }

    public void Begin(Transform attackOwner, int attackFacing)
    {
        owner = attackOwner;
        hitTargets.Clear();
        hitHealthTargets.Clear();
        SetFacing(attackFacing);
        DisableHitbox();
    }

    public void PlayParryCue(Transform fallbackPoint)
    {
        parry?.PlayCue(this, fallbackPoint);
    }

    public void SetFacing(int attackFacing)
    {
        facing = attackFacing >= 0 ? 1 : -1;

        if (!active)
            ApplyShape(meleeSize, meleeRightFacingOffset);
    }

    public void EnableMeleeHitbox()
    {
        EnableMeleeHitbox(true);
    }

    public void EnableMeleeHitbox(bool clearPreviousHits)
    {
        StopTimedWindow();
        StopChain();

        if (clearPreviousHits)
        {
            hitTargets.Clear();
            hitHealthTargets.Clear();
        }

        active = true;
        ApplyShape(meleeSize, meleeRightFacingOffset);

        if (attackCollider != null)
            attackCollider.enabled = true;
    }

    public void EnableTimedMeleeHitbox(float duration, bool clearPreviousHits)
    {
        StopTimedWindow();
        EnableMeleeHitbox(clearPreviousHits);

        if (duration > 0f)
            timedWindowRoutine = StartCoroutine(DisableAfter(duration));
    }

    public void PlayChain(float extendDuration, float holdDuration, float retractDuration)
    {
        StopTimedWindow();
        StopChain();
        hitTargets.Clear();
        hitHealthTargets.Clear();
        active = true;

        if (attackCollider != null)
            attackCollider.enabled = true;

        chainRoutine = StartCoroutine(AnimateChain(extendDuration, holdDuration, retractDuration));
    }

    public void DisableHitbox()
    {
        StopTimedWindow();
        StopChain();
        active = false;

        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    private IEnumerator DisableAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        timedWindowRoutine = null;
        active = false;

        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    private IEnumerator AnimateChain(float extendDuration, float holdDuration, float retractDuration)
    {
        yield return LerpChain(chainStartSize, chainExtendedSize, chainStartRightFacingOffset, chainExtendedRightFacingOffset, extendDuration, chainExtendCurve);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        yield return LerpChain(chainExtendedSize, chainStartSize, chainExtendedRightFacingOffset, chainStartRightFacingOffset, retractDuration, chainRetractCurve);

        chainRoutine = null;
        active = false;

        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    private IEnumerator LerpChain(Vector2 fromSize, Vector2 toSize, Vector2 fromOffset, Vector2 toOffset, float duration, AnimationCurve curve)
    {
        if (duration <= 0f)
        {
            ApplyShape(toSize, toOffset);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;
            ApplyShape(Vector2.LerpUnclamped(fromSize, toSize, t), Vector2.LerpUnclamped(fromOffset, toOffset, t));
            yield return null;
        }

        ApplyShape(toSize, toOffset);
    }

    private void StopTimedWindow()
    {
        if (timedWindowRoutine == null)
            return;

        StopCoroutine(timedWindowRoutine);
        timedWindowRoutine = null;
    }

    private void StopChain()
    {
        if (chainRoutine == null)
            return;

        StopCoroutine(chainRoutine);
        chainRoutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!active || other == null || hitTargets.Contains(other))
            return;

        if (other.GetComponent<PlayerMeleeHitbox>() != null)
            return;

        if (!CanTarget(other))
            return;

        Vector3 attackerPosition = owner != null ? owner.position : transform.position;
        if (parry != null && parry.TryParry(other, attackerPosition, this))
        {
            hitTargets.Add(other);
            DisableHitbox();
            return;
        }

        Health health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health == null)
            return;

        if (hitHealthTargets.Contains(health))
            return;

        hitTargets.Add(other);
        hitHealthTargets.Add(health);

        if (applyKnockback)
            health.TakeDamage(damage, owner != null ? owner.position : transform.position);
        else
            health.TakeDamage(damage);
    }

    private bool CanTarget(Collider2D other)
    {
        if (playerLayer.value != 0)
            return (playerLayer.value & (1 << other.gameObject.layer)) != 0;

        return other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
    }

    private void ApplyShape(Vector2 size, Vector2 rightFacingOffset)
    {
        if (attackCollider == null)
            return;

        attackCollider.size = size;
        attackCollider.offset = new Vector2(Mathf.Abs(rightFacingOffset.x) * facing, rightFacingOffset.y);
    }

    private void ApplyEnemyAttackLayer()
    {
        int enemyAttackLayer = LayerMask.NameToLayer(EnemyAttackLayerName);
        if (enemyAttackLayer >= 0 && gameObject.layer != enemyAttackLayer)
            gameObject.layer = enemyAttackLayer;
    }

    private void OnValidate()
    {
        ApplyEnemyAttackLayer();

        if (attackCollider == null)
            attackCollider = GetComponent<BoxCollider2D>();

        if (attackCollider == null)
            return;

        attackCollider.isTrigger = true;

        if (role == HitboxRole.Chain && previewChainShape)
        {
            Vector2 previewSize = Vector2.LerpUnclamped(chainStartSize, chainExtendedSize, chainPreviewProgress);
            Vector2 previewOffset = Vector2.LerpUnclamped(chainStartRightFacingOffset, chainExtendedRightFacingOffset, chainPreviewProgress);
            ApplyShape(previewSize, previewOffset);
            return;
        }

        ApplyShape(meleeSize, meleeRightFacingOffset);
    }

    private void OnDrawGizmosSelected()
    {
        if (role == HitboxRole.Melee)
        {
            DrawShape(meleeSize, meleeRightFacingOffset, Color.white);
            return;
        }

        DrawShape(chainStartSize, chainStartRightFacingOffset, Color.cyan);
        DrawShape(chainExtendedSize, chainExtendedRightFacingOffset, Color.yellow);
        Vector2 previewSize = Vector2.LerpUnclamped(chainStartSize, chainExtendedSize, chainPreviewProgress);
        Vector2 previewOffset = Vector2.LerpUnclamped(chainStartRightFacingOffset, chainExtendedRightFacingOffset, chainPreviewProgress);
        DrawShape(previewSize, previewOffset, Color.green);
    }

    private void DrawShape(Vector2 size, Vector2 rightFacingOffset, Color color)
    {
        Gizmos.color = color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(Mathf.Abs(rightFacingOffset.x) * facing, rightFacingOffset.y, 0f), size);
        Gizmos.matrix = previousMatrix;
    }
}
