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
            if (CanAttackPlayer()) // Saldýrý mesafesindeyse
            {
                if (cooldownTimer >= attackCooldown) // Saldýrý soðuma süresi dolmuþsa
                {
                    cooldownTimer = 0;
                    StartAttack(); // Saldýrýyý baþlat
                }
                else
                {
                    Idle();
                }
            }
            else
            {
                ChasePlayer(); // Saldýrý mesafesinde deðilse kovalama yap
            }

            // Eðer oyuncu görüþ alanýndan çýktýysa
            if (!CanChasePlayer())
            {
                StopChasing();
            }
        }
        else
        {
            // Eðer oyuncu görüþ alanýna girdiyse, kovalamaya baþla
            if (CanChasePlayer())
            {
                StartChasing();
            }
        }

        // Eðer düþman kovalamýyorsa, devriye moduna geç
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
        enemyPatrol.enabled = false; // Kovalamaya baþlarken devriye modunu kapat
        anim.SetBool("Moving", true); // Koþma animasyonunu baþlat
        anim.SetBool("Idle", false); // Idle animasyonunu durdur
    }

    private void StopChasing()
    {
        isChasing = false;
        anim.SetBool("Moving", false); // Koþma animasyonunu durdur
        anim.SetTrigger("Idle"); // Kovalamayý býrak, idle animasyonuna geç
        enemyPatrol.enabled = true; // Kovalamayý býrak, devriye moduna geç
    }

    private void ChasePlayer()
    {
            anim.SetBool("Moving", true);
            anim.SetBool("Idle", false);// Koþma animasyonu devam eder
            // Düþmaný oyuncuya doðru hareket ettir
            transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);

            // Yüzü oyuncuya doðru çevir
            if ((player.position.x - transform.position.x) > 0)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void StartAttack()
    {
        isAttacking = true;
        anim.SetTrigger("meleeAttackTrigger");
        anim.SetBool("meleeAttackBool", true);// Saldýrý tetikleyicisini ayarla
    }


    private bool CanChasePlayer()
    {
        // Ön raycast - sadece kovalamaca sýrasýnda aktif
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

        Gizmos.color = Color.red; // Saldýrý mesafesi
        Gizmos.DrawWireCube(boxCollider.bounds.center + transform.right * (1 + attackRange / 2f) * transform.localScale.x * boxCollider.bounds.size.x,
            new Vector3(boxCollider.bounds.size.x * attackRange, boxCollider.bounds.size.y, boxCollider.bounds.size.z));
    }

    public void DamagePlayer()
    {
        if (playerHealth != null && isAttacking)
        {
            playerHealth.TakeDamage(damage);
        }
        isAttacking = false; // Saldýrý sonrasý normal moda dön

        if (isChasing)
        {
            anim.SetBool("Moving", true); // Eðer kovalama modundaysa tekrar koþma animasyonuna geç
        }
        else
        {
            anim.SetBool("Idle", true); // Saldýrý sonrasý idle animasyonuna geç
        }
    }
}
