using System.Collections.Generic;
using UnityEngine;

public class BloodKnightAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Collider2D attackCollider;
    [SerializeField] private bool mirrorColliderOffset = true;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;

    [Header("Parry")]
    [SerializeField] private bool canBeParried = true;

    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    private Transform owner;
    private Vector2 baseColliderOffset;
    private bool active;

    public bool WasParried { get; private set; }

    private void Awake()
    {
        if (attackCollider == null)
            attackCollider = GetComponent<Collider2D>();

        if (attackCollider != null)
        {
            baseColliderOffset = attackCollider.offset;
            attackCollider.isTrigger = true;
            attackCollider.enabled = false;
        }
    }

    public void Begin(Transform attackOwner, Collider2D _, int __)
    {
        owner = attackOwner;
        WasParried = false;
        hitTargets.Clear();
        DisableHitbox();
        SetFacing(__);
    }

    public void SetFacing(int facing)
    {
        if (!mirrorColliderOffset || attackCollider == null)
            return;

        int direction = facing >= 0 ? 1 : -1;
        attackCollider.offset = new Vector2(Mathf.Abs(baseColliderOffset.x) * direction, baseColliderOffset.y);
    }

    public void EnableHitbox()
    {
        active = true;
        hitTargets.Clear();

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

        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (TryParry(other))
        {
            hitTargets.Add(other);
            DisableHitbox();
            return;
        }

        Health health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health == null)
            return;

        hitTargets.Add(other);
        health.TakeDamage(damage);
    }

    private bool TryParry(Collider2D playerHit)
    {
        if (!canBeParried)
            return false;

        PlayerParry parry = playerHit.GetComponent<PlayerParry>() ?? playerHit.GetComponentInParent<PlayerParry>();
        if (parry == null || !parry.TryParry(owner != null ? owner.position : transform.position))
            return false;

        WasParried = true;
        return true;
    }
}
