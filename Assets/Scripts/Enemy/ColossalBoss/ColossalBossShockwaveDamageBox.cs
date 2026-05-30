using System.Collections.Generic;
using UnityEngine;

public class ColossalBossShockwaveDamageBox : MonoBehaviour
{
    private HashSet<Health> hitTargets = new HashSet<Health>();

    private int damage;
    private Vector3 damageSource;
    private bool active;

    public void Initialize(int hitDamage, Vector3 source, HashSet<Health> sharedHitTargets = null)
    {
        damage = hitDamage;
        damageSource = source;
        hitTargets = sharedHitTargets ?? new HashSet<Health>();
        active = true;
    }

    private void OnDisable()
    {
        active = false;
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
        if (!active || other == null)
            return;

        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerMovement>() == null)
            return;

        Health targetHealth = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (targetHealth == null || hitTargets.Contains(targetHealth))
            return;

        hitTargets.Add(targetHealth);
        targetHealth.TakeDamage(damage, damageSource);
    }
}
