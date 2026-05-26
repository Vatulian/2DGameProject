using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMeleeHitbox : MonoBehaviour
{
    private const string EnemyAttackLayerName = "EnemyAttack";

    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private readonly HashSet<Health> hitHealthTargets = new HashSet<Health>();
    private readonly HashSet<Boss> hitBossTargets = new HashSet<Boss>();

    private BoxCollider2D hitbox;
    private int damage = 1;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
    }

    public void Configure(int phaseDamage, Vector2 phaseHitboxSize, Vector2 phaseHitboxOffset)
    {
        Configure(phaseDamage, phaseHitboxSize, phaseHitboxOffset, PlayerMeleeHitboxAnchor.Root);
    }

    public void Configure(int phaseDamage, Vector2 phaseHitboxSize, Vector2 phaseHitboxOffset, PlayerMeleeHitboxAnchor _)
    {
        Configure(phaseDamage, phaseHitboxSize, phaseHitboxOffset, _, GetFacingSign());
    }

    public void Configure(int phaseDamage, Vector2 phaseHitboxSize, Vector2 phaseHitboxOffset, PlayerMeleeHitboxAnchor _, float facingSign)
    {
        damage = phaseDamage;
        hitbox.size = phaseHitboxSize;
        hitbox.offset = new Vector2(phaseHitboxOffset.x * GetSafeSign(facingSign), phaseHitboxOffset.y);
    }

    private float GetFacingSign()
    {
        PlayerMovement movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
            return movement.IsFacingRight ? 1f : -1f;

        return GetSafeSign(transform.root.localScale.x);
    }

    private static float GetSafeSign(float value)
    {
        return value < 0f ? -1f : 1f;
    }

    public void BeginSwing()
    {
        BeginSwing(true);
    }

    public void BeginSwing(bool clearPreviousHits)
    {
        if (clearPreviousHits)
        {
            hitTargets.Clear();
            hitHealthTargets.Clear();
            hitBossTargets.Clear();
        }

        hitbox.enabled = true;
    }

    public void EndSwing()
    {
        hitbox.enabled = false;
        hitTargets.Clear();
        hitHealthTargets.Clear();
        hitBossTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (!hitbox.enabled || other == null || hitTargets.Contains(other))
            return;

        if (IsEnemyAttackCollider(other))
            return;

        Health health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health != null && health.gameObject != transform.root.gameObject)
        {
            if (hitHealthTargets.Contains(health))
                return;

            Vector3 hitPoint = other.bounds.ClosestPoint(transform.position);
            hitTargets.Add(other);
            hitHealthTargets.Add(health);
            health.TakeDamageAt(damage, hitPoint);

            if (!HasBossDamageFeedback(health))
            {
                HitFlash flash = health.GetComponentInChildren<HitFlash>();
                if (flash != null)
                    flash.Play();
            }

            return;
        }

        Boss boss = other.GetComponent<Boss>() ?? other.GetComponentInParent<Boss>();
        if (boss != null)
        {
            if (hitBossTargets.Contains(boss))
                return;

            hitTargets.Add(other);
            hitBossTargets.Add(boss);
            boss.TakeDamageAt(damage, other.bounds.ClosestPoint(transform.position));
        }
    }

    private static bool HasBossDamageFeedback(Health health)
    {
        return health.GetComponentInParent<BossDamageFeedback>() != null
               || health.GetComponentInChildren<BossDamageFeedback>() != null;
    }

    private static bool IsEnemyAttackCollider(Collider2D other)
    {
        int enemyAttackLayer = LayerMask.NameToLayer(EnemyAttackLayerName);
        if (enemyAttackLayer >= 0 && other.gameObject.layer == enemyAttackLayer)
            return true;

        return other.GetComponentInParent<EliteKillerAttackHitbox>() != null
               || other.GetComponentInParent<BloodKnightAttack>() != null;
    }
}

public enum PlayerMeleeHitboxAnchor
{
    Root,
    Origin
}
