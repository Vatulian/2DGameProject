using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMeleeHitbox : MonoBehaviour
{
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

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
        damage = phaseDamage;
        hitbox.size = phaseHitboxSize;
        hitbox.offset = phaseHitboxOffset;
    }

    public void BeginSwing()
    {
        BeginSwing(true);
    }

    public void BeginSwing(bool clearPreviousHits)
    {
        if (clearPreviousHits)
            hitTargets.Clear();

        hitbox.enabled = true;
    }

    public void EndSwing()
    {
        hitbox.enabled = false;
        hitTargets.Clear();
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

        Health health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health != null && health.gameObject != transform.root.gameObject)
        {
            hitTargets.Add(other);
            health.TakeDamage(damage);

            HitFlash flash = health.GetComponentInChildren<HitFlash>();
            if (flash != null)
                flash.Play();

            return;
        }

        Boss boss = other.GetComponent<Boss>() ?? other.GetComponentInParent<Boss>();
        if (boss != null)
        {
            hitTargets.Add(other);
            boss.TakeDamageAt(damage, other.bounds.ClosestPoint(transform.position));
        }
    }
}

public enum PlayerMeleeHitboxAnchor
{
    Root,
    Origin
}
