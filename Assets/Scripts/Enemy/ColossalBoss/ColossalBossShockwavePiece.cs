using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColossalBossShockwavePiece : MonoBehaviour
{
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private Animator animator;

    public void Configure(
        ColossalBossShockwaveSettings settings,
        Vector3 damageSource,
        HashSet<Health> sharedHitTargets)
    {
        if (settings == null)
            return;

        ResolveReferences();
        ConfigureDamage(settings, damageSource, sharedHitTargets);
        PlayAnimation(settings);

        StartCoroutine(DisableColliderAfter(settings.pieceColliderLifetime));
        Destroy(gameObject, settings.pieceLifetime);
    }

    private void ResolveReferences()
    {
        if (damageCollider == null)
            damageCollider = GetComponentInChildren<Collider2D>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void ConfigureDamage(ColossalBossShockwaveSettings settings, Vector3 damageSource, HashSet<Health> sharedHitTargets)
    {
        if (damageCollider == null)
            return;

        int enemyAttackLayer = LayerMask.NameToLayer("EnemyAttack");
        if (enemyAttackLayer >= 0)
            damageCollider.gameObject.layer = enemyAttackLayer;

        damageCollider.isTrigger = true;
        damageCollider.enabled = true;

        ColossalBossShockwaveDamageBox damageBox = damageCollider.GetComponent<ColossalBossShockwaveDamageBox>();
        if (damageBox == null)
            damageBox = damageCollider.gameObject.AddComponent<ColossalBossShockwaveDamageBox>();

        damageBox.Initialize(settings.damage, damageSource, sharedHitTargets);
    }

    private void PlayAnimation(ColossalBossShockwaveSettings settings)
    {
        if (animator == null)
            return;

        if (!string.IsNullOrWhiteSpace(settings.visualStateName))
            animator.Play(settings.visualStateName, 0, 0f);
    }

    private IEnumerator DisableColliderAfter(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (damageCollider != null)
            damageCollider.enabled = false;
    }
}
