using UnityEngine;

public class EnemyTouchDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int touchDamage = 1;
    [SerializeField] private float touchCooldown = 0.5f;

    private float nextAllowedTime;

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D col)
    {
        if (Time.time < nextAllowedTime) return;
        if (!col.CompareTag("Player")) return;

        Health hp = col.GetComponent<Health>();
        if (hp == null) return;

        // Player i-frame'de ise zaten Health içi engelliyor (invulnerable)
        hp.TakeDamage(touchDamage);

        nextAllowedTime = Time.time + touchCooldown;
    }
}