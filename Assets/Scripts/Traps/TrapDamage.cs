using System.Collections.Generic;
using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float damageCooldown = 0.35f;

    private readonly Dictionary<Health, float> nextDamageTimes = new Dictionary<Health, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other, other.bounds.center);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other, other.bounds.center);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider, GetContactPoint(collision));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider, GetContactPoint(collision));
    }

    private void TryDamage(Collider2D other, Vector3 hitPoint)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        Health health = other.GetComponent<Health>();
        if (health == null)
            return;

        if (nextDamageTimes.TryGetValue(health, out float nextAllowedTime) && Time.time < nextAllowedTime)
            return;

        Vector3 knockbackSource = GetKnockbackSource(other, hitPoint);
        health.TakeDamageAt(damage, hitPoint, knockbackSource);
        nextDamageTimes[health] = Time.time + damageCooldown;
    }

    private Vector3 GetContactPoint(Collision2D collision)
    {
        if (collision.contactCount <= 0)
            return collision.collider != null ? collision.collider.bounds.center : transform.position;

        return collision.GetContact(0).point;
    }

    private Vector3 GetKnockbackSource(Collider2D target, Vector3 hitPoint)
    {
        Collider2D trapCollider = GetComponent<Collider2D>();
        if (trapCollider != null)
            hitPoint = trapCollider.ClosestPoint(target.bounds.center);

        if (Mathf.Abs(target.transform.position.x - hitPoint.x) < 0.01f)
        {
            float fallbackDirection = target.transform.position.x >= transform.position.x ? -1f : 1f;
            hitPoint = target.transform.position + Vector3.right * fallbackDirection * 0.1f;
        }

        return hitPoint;
    }
}
