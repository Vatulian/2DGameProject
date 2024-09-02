using System;
using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    [Header("Attack Parameters")]
    [SerializeField] private float attackCooldown;
    [SerializeField] private float attackRange;
    [SerializeField] private int damage;

    [Header("Chase Parameters")]
    [SerializeField] private float chaseRange;
    [SerializeField] private float chaseSpeed;

    [Header("Collider Parameters")]
    [SerializeField] private float colliderDistance;
    [SerializeField] private BoxCollider2D boxCollider;

    [Header("Player Layer")]
    [SerializeField] private LayerMask playerLayer;
    private float cooldownTimer = Mathf.Infinity;

    // References
    private Animator anim;
    private Health playerHealth;
    private EnemyPatrol enemyPatrol;
    private Transform player;

    // States
    private bool isChasing;
    private bool isAttacking;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        enemyPatrol = GetComponentInParent<EnemyPatrol>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (isChasing)
        {
            if (CanAttackPlayer()) // Sald�r� mesafesindeyse
            {
                if (cooldownTimer >= attackCooldown) // Sald�r� so�uma s�resi dolmu�sa
                {
                    cooldownTimer = 0;
                    StartAttack(); // Sald�r�y� ba�lat
                }
                else
                {
                    Idle();
                }
            }
            else
            {
                ChasePlayer(); // Sald�r� mesafesinde de�ilse kovalama yap
            }

            // E�er oyuncu g�r�� alan�ndan ��kt�ysa
            if (!CanChasePlayer())
            {
                StopChasing();
            }
        }
        else
        {
            // E�er oyuncu g�r�� alan�na girdiyse, kovalamaya ba�la
            if (CanChasePlayer())
            {
                StartChasing();
            }
        }

        // E�er d��man kovalam�yorsa, devriye moduna ge�
        if (!isChasing && enemyPatrol != null && !isAttacking)
        {
            enemyPatrol.enabled = true;
        }
    }

    private void Idle()
    {
        anim.SetBool("Moving", false);
        anim.SetBool("Idle", true);
    }

    private void StartChasing()
    {
        isChasing = true;
        enemyPatrol.enabled = false; // Kovalamaya ba�larken devriye modunu kapat
        anim.SetBool("Moving", true); // Ko�ma animasyonunu ba�lat
        anim.SetBool("Idle", false); // Idle animasyonunu durdur
    }

    private void StopChasing()
    {
        isChasing = false;
        anim.SetBool("Moving", false); // Ko�ma animasyonunu durdur
        anim.SetTrigger("Idle"); // Kovalamay� b�rak, idle animasyonuna ge�
        enemyPatrol.enabled = true; // Kovalamay� b�rak, devriye moduna ge�
    }

    private void ChasePlayer()
    {
            anim.SetBool("Moving", true);
            anim.SetBool("Idle", false);// Ko�ma animasyonu devam eder
            // D��man� oyuncuya do�ru hareket ettir
            transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);

            // Y�z� oyuncuya do�ru �evir
            if ((player.position.x - transform.position.x) > 0)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void StartAttack()
    {
        isAttacking = true;
        anim.SetTrigger("meleeAttackTrigger");
        anim.SetBool("meleeAttackBool", true);// Sald�r� tetikleyicisini ayarla
    }


    private bool CanChasePlayer()
    {
        // �n raycast - sadece kovalamaca s�ras�nda aktif
        RaycastHit2D hitForward = Physics2D.BoxCast(boxCollider.bounds.center + transform.right * transform.localScale.x * colliderDistance,
            new Vector3(boxCollider.bounds.size.x * chaseRange, boxCollider.bounds.size.y, boxCollider.bounds.size.z),
            0, Vector2.left, 0, playerLayer);

        return hitForward.collider != null;
    }

    private bool CanAttackPlayer()
    {
        RaycastHit2D hit = Physics2D.BoxCast(boxCollider.bounds.center + transform.right * (1 + attackRange / 2f) * transform.localScale.x * boxCollider.bounds.size.x,
            new Vector3(boxCollider.bounds.size.x * attackRange, boxCollider.bounds.size.y, boxCollider.bounds.size.z),
            0, Vector2.left, 0, playerLayer);

        if (hit.collider != null)
        {
            playerHealth = hit.transform.GetComponent<Health>();
        }

        return hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        Debug.Log("Center");
        Debug.Log(transform.right * (attackRange / 4f) * transform.localScale.x);
        Debug.Log("Size");
        Debug.Log(boxCollider.bounds.size.x * attackRange);
        Gizmos.color = Color.yellow; // Kovalama mesafesi
        Gizmos.DrawWireCube(boxCollider.bounds.center + transform.right * transform.localScale.x * colliderDistance,
            new Vector3(boxCollider.bounds.size.x * chaseRange, boxCollider.bounds.size.y, boxCollider.bounds.size.z));

        Gizmos.color = Color.red; // Sald�r� mesafesi
        Gizmos.DrawWireCube(boxCollider.bounds.center + transform.right * (1 + attackRange / 2f) * transform.localScale.x * boxCollider.bounds.size.x,
            new Vector3(boxCollider.bounds.size.x * attackRange, boxCollider.bounds.size.y, boxCollider.bounds.size.z));
    }

    public void DamagePlayer()
    {
        if (playerHealth != null && isAttacking)
        {
            playerHealth.TakeDamage(damage);
        }
        isAttacking = false; // Sald�r� sonras� normal moda d�n

        if (isChasing)
        {
            anim.SetBool("Moving", true); // E�er kovalama modundaysa tekrar ko�ma animasyonuna ge�
        }
        else
        {
            anim.SetBool("Idle", true); // Sald�r� sonras� idle animasyonuna ge�
        }
    }
}
