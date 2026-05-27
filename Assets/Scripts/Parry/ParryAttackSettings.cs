using System;
using UnityEngine;

[Serializable]
public class ParryAttackSettings
{
    [SerializeField] private bool canBeParried = true;
    [SerializeField] private Transform cuePoint;
    [SerializeField] private Vector3 cueOffset;
    [SerializeField] private GameObject parryableCuePrefab;
    [SerializeField] private GameObject unparryableCuePrefab;
    [SerializeField] private float cueLifetime = 1f;

    public bool CanBeParried => canBeParried;

    public bool TryParry(Collider2D playerHit, Vector3 attackerPosition, Component parrySource)
    {
        return PlayerParry.TryParryHit(playerHit, attackerPosition, canBeParried, parrySource);
    }

    public void PlayCue(Component owner, Transform fallbackPoint = null)
    {
        GameObject prefab = canBeParried ? parryableCuePrefab : unparryableCuePrefab;
        if (prefab == null)
            return;

        Transform spawnPoint = cuePoint != null ? cuePoint : fallbackPoint;
        Vector3 position = spawnPoint != null ? spawnPoint.position : owner.transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : owner.transform.rotation;
        GameObject cue = UnityEngine.Object.Instantiate(prefab, position + cueOffset, rotation);

        if (cueLifetime > 0f)
            UnityEngine.Object.Destroy(cue, cueLifetime);
    }
}
