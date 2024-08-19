using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;  // Default cooldown time
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;

    private Animator anim;
    private PlayerMovement playerMovement;
    private float cooldownTimer = Mathf.Infinity;
    private bool isAttacking;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (Input.GetMouseButton(0) && cooldownTimer >= attackCooldown && playerMovement.canAttack())
        {
            if (!isAttacking) // Ensure that Attack is not called multiple times
            {
                isAttacking = true;
                Attack();
            }
        }
        else
        {
            isAttacking = false;
        }
    }

    private void Attack()
    {
        anim.SetTrigger("Attack");
        cooldownTimer = 0f;

        int fireballIndex = FindFireball();

        if (fireballIndex >= 0)
        {
            GameObject fireball = fireballs[fireballIndex];
            fireball.transform.position = firePoint.position;
            fireball.GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
        }
    }

    private int FindFireball()
    {
        for (int i = 0; i < fireballs.Length; i++)
        {
            if (!fireballs[i].activeInHierarchy)
                return i;
        }
        return -1; // Return -1 if no available fireball is found
    }
}
