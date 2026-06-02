using UnityEngine;

public class HazardRecoveryPoint : MonoBehaviour
{
    [SerializeField] private Transform recoveryPoint;
    [SerializeField] private bool updateWhilePlayerStays;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrySetRecoveryPoint(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (updateWhilePlayerStays)
            TrySetRecoveryPoint(other);
    }

    private void TrySetRecoveryPoint(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null)
            return;

        Transform target = recoveryPoint != null ? recoveryPoint : transform;
        respawn.SetHazardRecoveryPosition(target.position);
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = recoveryPoint != null ? recoveryPoint : transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, 0.18f);
        Gizmos.DrawLine(transform.position, target.position);
    }
}
