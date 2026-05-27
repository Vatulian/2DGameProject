using System.Collections.Generic;
using UnityEngine;

public class BloodKnightAttack : MonoBehaviour
{
    private const string EnemyAttackLayerName = "EnemyAttack";

    [Header("Refs")]
    [SerializeField] private BoxCollider2D attackCollider;

    [Header("Hitbox")]
    [SerializeField] private Vector2 hitboxSize = new Vector2(1.1f, 1f);
    [SerializeField] private Vector2 rightFacingOffset = new Vector2(0.8f, 0f);

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

    public float ForwardReach => Mathf.Abs(rightFacingOffset.x) + hitboxSize.x * 0.5f;

    private void Awake()
    {
        ApplyEnemyAttackLayer();

        if (attackCollider == null)
            attackCollider = GetComponent<BoxCollider2D>();

        if (attackCollider != null)
        {
            attackCollider.isTrigger = true;
            attackCollider.enabled = false;
            ApplyHitboxShape();
        }
    }

    public void Begin(Transform attackOwner, Collider2D _, int __)
    {
        owner = attackOwner;
        hitTargets.Clear();
        hitHealthTargets.Clear();
        DisableHitbox();
        SetFacing(__);
    }

    public void PlayParryCue(Transform fallbackPoint)
    {
        parry?.PlayCue(this, fallbackPoint);
    }

    public void SetFacing(int facing)
    {
        this.facing = facing >= 0 ? 1 : -1;
        ApplyHitboxShape();
    }

    public void EnableHitbox()
    {
        active = true;
        hitTargets.Clear();
        hitHealthTargets.Clear();

        if (attackCollider != null)
            attackCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        active = false;

        if (attackCollider != null)
            attackCollider.enabled = false;
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

        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
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

    private void ApplyHitboxShape()
    {
        if (attackCollider == null)
            return;

        attackCollider.size = hitboxSize;
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

        ApplyHitboxShape();
    }
}
