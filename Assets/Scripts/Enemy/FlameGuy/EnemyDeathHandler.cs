using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Animator anim;     // Sprite child
    [SerializeField] private string deathState = "death";
    [SerializeField] private float destroyDelay = 0.1f;

    private bool dead;

    private void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!anim) anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (dead || health == null) return;

        if (health.currentHealth <= 0f)
        {
            dead = true;

            if (anim != null)
                anim.Play(deathState);

            Destroy(gameObject, destroyDelay);
        }
    }
}