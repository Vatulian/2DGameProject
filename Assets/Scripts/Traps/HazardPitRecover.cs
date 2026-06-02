using System.Collections.Generic;
using UnityEngine;

public class HazardPitRecover : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float recoveryCooldown = 0.5f;

    private readonly Dictionary<PlayerRespawn, float> nextRecoveryTimes = new Dictionary<PlayerRespawn, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRecover(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryRecover(other);
    }

    private void TryRecover(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null)
            return;

        if (nextRecoveryTimes.TryGetValue(respawn, out float nextAllowedTime) && Time.time < nextAllowedTime)
            return;

        nextRecoveryTimes[respawn] = Time.time + recoveryCooldown;
        respawn.RecoverFromHazard(damage);
    }
}
