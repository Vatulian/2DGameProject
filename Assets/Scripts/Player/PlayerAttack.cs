using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;
    [SerializeField] private AudioClip fireballSound;

    private PlayerAnimationController animationController;
    private Animator anim;
    private PlayerMovement playerMovement;
    private Health health;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        animationController = GetComponent<PlayerAnimationController>();
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (health != null && health.IsDead)
            return;

        if (Input.GetMouseButtonDown(0) && cooldownTimer <= 0f && playerMovement != null && playerMovement.canAttack())
            FireOnce();
    }

    private void FireOnce()
    {
        if (animationController == null)
            animationController = GetComponent<PlayerAnimationController>();

        int idx = FindInactiveFireball();
        if (idx < 0)
            return;

        cooldownTimer = attackCooldown;

        if (animationController != null) animationController.PlayAttack();
        else if (anim != null) anim.SetTrigger("Attack");

        if (SoundManager.instance && fireballSound)
            SoundManager.instance.PlaySound(fireballSound);

        GameObject go = fireballs[idx];
        go.transform.position = firePoint.position;

        float dir = Mathf.Sign(transform.localScale.x);
        Projectile proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Fire(transform, dir);
        else
            go.GetComponent<Projectile>()?.SetDirection(dir);
    }

    private int FindInactiveFireball()
    {
        for (int i = 0; i < fireballs.Length; i++)
            if (!fireballs[i].activeInHierarchy)
                return i;

        return -1;
    }
}
