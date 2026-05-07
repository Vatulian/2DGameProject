using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Animator anim;     // Sprite child
    [SerializeField] private string deathState = "death";
    [SerializeField] private float destroyDelay = 0.1f;

    private bool dead;
    private Collider2D[] colliders;
    private Rigidbody2D rb;

    private void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (dead || health == null) return;

        if (health.currentHealth <= 0f)
        {
            dead = true;
            DisableCollisionAndPhysics();

            if (anim != null)
                anim.Play(deathState);

            Destroy(gameObject, destroyDelay);
        }
    }

    private void DisableCollisionAndPhysics()
    {
        if (colliders != null)
        {
            foreach (Collider2D col in colliders)
            {
                if (col != null)
                    col.enabled = false;
            }
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }
}
