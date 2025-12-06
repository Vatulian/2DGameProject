using UnityEngine;

public class FlyingEnemyAnimation : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Animator anim;

    private float lastHealth;
    private bool dead;

    private void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!anim) anim = GetComponentInChildren<Animator>();

        if (health != null)
            lastHealth = health.currentHealth;
    }

    private void Update()
    {
        if (health == null || anim == null) return;

        // Ölüm
        if (!dead && health.currentHealth <= 0)
        {
            dead = true;
            anim.SetTrigger("DieTrigger");
            return;
        }

        // Hit
        if (!dead && health.currentHealth < lastHealth)
        {
            anim.SetTrigger("Hit");
        }

        lastHealth = health.currentHealth;
    }

    public void PlayAttack()
    {
        if (!dead)
            anim.SetTrigger("AttackTrigger");
    }
}