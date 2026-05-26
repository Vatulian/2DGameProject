using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private float attackLockDuration = 0.2f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private KeyCode attackKey = KeyCode.F;

    private PlayerAnimationController animationController;
    private Animator anim;
    private PlayerMovement playerMovement;
    private PlayerMeleeAttack meleeAttack;
    private Health health;
    private float cooldownTimer = 0f;
    private float attackLockTimer = 0f;
    private bool triedAutoAssignFireballs;

    public bool IsAttacking => attackLockTimer > 0f;

    private void Awake()
    {
        animationController = GetComponentInChildren<PlayerAnimationController>();
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        meleeAttack = GetComponent<PlayerMeleeAttack>();
        health = GetComponent<Health>();
        EnsureFireballsAssigned();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        attackLockTimer -= Time.deltaTime;

        if (health != null && health.IsDead)
            return;

        if (Input.GetKeyDown(attackKey)
            && cooldownTimer <= 0f
            && playerMovement != null
            && playerMovement.canAttack()
            && (meleeAttack == null || !meleeAttack.IsAttacking))
        {
            FireOnce();
        }
    }

    private void FireOnce()
    {
        if (animationController == null)
            animationController = GetComponentInChildren<PlayerAnimationController>();

        int idx = FindInactiveFireball();
        if (idx < 0)
            return;

        cooldownTimer = attackCooldown;
        attackLockTimer = Mathf.Max(attackLockDuration, attackCooldown * 0.5f);

        if (animationController != null) animationController.PlayAttack();
        else if (anim != null) anim.SetTrigger("Attack");

        if (SoundManager.instance && fireballSound)
            SoundManager.instance.PlaySound(fireballSound);

        GameObject go = fireballs[idx];
        float dir = GetFacingDirection();
        go.transform.position = GetFirePointPosition(dir);

        Projectile proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Fire(transform, dir);
        else
            go.GetComponent<Projectile>()?.SetDirection(dir);
    }

    private float GetFacingDirection()
    {
        if (playerMovement != null)
            return playerMovement.IsFacingRight ? 1f : -1f;

        return 1f;
    }

    private Vector3 GetFirePointPosition(float direction)
    {
        if (firePoint == null)
            return transform.position;

        if (!firePoint.IsChildOf(transform))
            return firePoint.position;

        Vector3 localPosition = transform.InverseTransformPoint(firePoint.position);
        localPosition.x = Mathf.Abs(localPosition.x) * Mathf.Sign(direction);
        return transform.TransformPoint(localPosition);
    }

    private int FindInactiveFireball()
    {
        EnsureFireballsAssigned();

        if (fireballs == null || fireballs.Length == 0)
            return -1;

        for (int i = 0; i < fireballs.Length; i++)
            if (fireballs[i] != null && !fireballs[i].activeInHierarchy)
                return i;

        return -1;
    }

    private void EnsureFireballsAssigned()
    {
        if (!HasMissingFireballReferences())
            return;

        if (triedAutoAssignFireballs)
            return;

        triedAutoAssignFireballs = true;

        Projectile[] projectiles = Resources.FindObjectsOfTypeAll<Projectile>();
        if (projectiles == null || projectiles.Length == 0)
            return;

        int count = 0;
        for (int i = 0; i < projectiles.Length; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile != null && projectile.gameObject.scene.IsValid())
                count++;
        }

        if (count == 0)
            return;

        fireballs = new GameObject[count];
        int index = 0;

        for (int i = 0; i < projectiles.Length; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile != null && projectile.gameObject.scene.IsValid())
                fireballs[index++] = projectile.gameObject;
        }
    }

    private bool HasMissingFireballReferences()
    {
        if (fireballs == null || fireballs.Length == 0)
            return true;

        for (int i = 0; i < fireballs.Length; i++)
        {
            if (fireballs[i] == null)
                return true;
        }

        return false;
    }
}
