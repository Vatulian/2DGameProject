using UnityEngine;

public class ExplodingTrap : MonoBehaviour
{
    public enum TrapMode
    {
        Homing,          // Görünce seni kovalar
        DropAlongSight   // Görünce, o anki doğrultuda bir yere çarpana kadar gider
    }

    private enum State
    {
        Idle,
        Windup,
        Active,
        Exploded
    }

    [Header("Mode")]
    [SerializeField] private TrapMode mode = TrapMode.Homing;

    [Header("Refs")]
    [SerializeField] private Transform model;            // Sprite/anim child (sadece görsel için)
    [SerializeField] private Transform visionOrigin;     // FOV için başlangıç noktası (yoksa kendi pozisyonunu kullanır)

    [Header("Animation")]
    [SerializeField] private Animator animator;          
    [SerializeField] private string explodeTriggerName = "Explode";
    [SerializeField] private float destroyDelayAfterExplosion = 0.5f;

    [Header("Detection (FOV)")]
    [SerializeField] private float viewDistance = 8f;
    [Tooltip("İleri yönün sol tarafına doğru maksimum açı (pozitif, derece)")]
    [SerializeField] private float viewAngleLeft = 45f;
    [Tooltip("İleri yönün sağ tarafına doğru maksimum açı (pozitif, derece)")]
    [SerializeField] private float viewAngleRight = 45f;
    [SerializeField] private LayerMask playerLayer;             
    [SerializeField] private LayerMask obstructionLayers;       

    [Header("FOV Orientation")]
    [Tooltip("FOV konisinin merkez yönü. Örn: (1,0)=sağ, (-1,0)=sol, (0,1)=yukarı, (0,-1)=aşağı")]
    [SerializeField] private Vector2 forwardDirection = Vector2.right;

    [Header("Timing")]
    [SerializeField] private float windupTime = 0.4f;           
    [SerializeField] private float activeDuration = 1.5f;       

    [Header("Movement")]
    [SerializeField] private float homingSpeed = 8f;            
    [SerializeField] private float dropSpeed = 12f;             

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 1.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject explosionPrefab;        
    [SerializeField] private AudioClip explosionSfx;            

    [Header("Collision")]
    [SerializeField] private LayerMask explodeOnCollisionLayers; 

    private State state = State.Idle;
    private float stateTimer;

    private Rigidbody2D rb;
    private Transform player;
    private bool hasExploded = false;

    // Yeni: DropAlongSight için, gördüğü andaki yön
    private Vector2 dropDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (visionOrigin == null)
            visionOrigin = transform;

        if (animator == null)
        {
            if (model != null)
                animator = model.GetComponent<Animator>();
            else
                animator = GetComponent<Animator>();
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (state == State.Exploded) return;

        switch (state)
        {
            case State.Idle:
                if (CanSeePlayer())
                    StartWindup();
                break;

            case State.Windup:
                HandleWindup();
                break;

            case State.Active:
                HandleActive();
                break;
        }
    }

    #region State Logic

    private void StartWindup()
    {
        if (player == null) return;

        // DropAlongSight: beni gördüğü anda doğrultuyu kaydet
        if (mode == TrapMode.DropAlongSight)
        {
            Vector2 toPlayer = (player.position - transform.position);
            if (toPlayer.sqrMagnitude > 0.0001f)
                dropDirection = toPlayer.normalized;
            else
                dropDirection = GetForward(); // fallback
        }

        state = State.Windup;
        stateTimer = windupTime;

        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void HandleWindup()
    {
        stateTimer -= Time.deltaTime;

        if (rb != null) rb.velocity = Vector2.zero;

        if (stateTimer <= 0f)
        {
            state = State.Active;
            stateTimer = activeDuration;
        }
    }

    private void HandleActive()
    {
        stateTimer -= Time.deltaTime;

        if (mode == TrapMode.Homing)
        {
            if (player != null)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                Move(dir, homingSpeed);
            }
        }
        else // DropAlongSight
        {
            if (dropDirection.sqrMagnitude < 0.0001f)
                dropDirection = GetForward(); // güvenlik

            Move(dropDirection, dropSpeed);
            // Çarpınca patlamayı OnCollisionEnter2D hallediyor
        }

        // Failsafe: hiç bir yere çarpmazsa süre bitince patla
        if (stateTimer <= 0f)
        {
            Explode();
        }
    }

    #endregion

    #region Movement helpers

    private void Move(Vector2 dir, float speed)
    {
        if (rb != null)
        {
            rb.velocity = dir * speed;
        }
        else
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
        }
    }

    #endregion

    #region Detection & Explosion

    private Vector2 GetForward()
    {
        if (forwardDirection.sqrMagnitude > 0.0001f)
            return forwardDirection.normalized;

        return Vector2.right;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = visionOrigin.position;
        Vector2 toPlayer = player.position - origin;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;

        Vector2 dirToPlayer = toPlayer.normalized;
        Vector2 forward = GetForward();

        float signedAngle = Vector2.SignedAngle(forward, dirToPlayer);

        if (signedAngle > viewAngleLeft || signedAngle < -viewAngleRight)
            return false;

        int mask = obstructionLayers | playerLayer;
        RaycastHit2D hit = Physics2D.Raycast(origin, dirToPlayer, dist, mask);
        if (hit && !hit.collider.CompareTag("Player"))
            return false;

        return true;
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        state = State.Exploded;

        if (rb != null) rb.velocity = Vector2.zero;

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (explosionRadius > 0f && damage > 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayer);
            foreach (var c in hits)
            {
                if (c.CompareTag("Player"))
                {
                    Health hp = c.GetComponent<Health>();
                    if (hp != null)
                        hp.TakeDamage(damage);
                }
            }
        }

        if (animator != null && !string.IsNullOrEmpty(explodeTriggerName))
            animator.SetTrigger(explodeTriggerName);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (explosionSfx != null && SoundManager.instance != null)
            SoundManager.instance.PlaySound(explosionSfx);

        Destroy(gameObject, destroyDelayAfterExplosion);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded) return;

        if (collision.collider.CompareTag("Player"))
        {
            Explode();
            return;
        }

        if (((1 << collision.gameObject.layer) & explodeOnCollisionLayers) != 0)
        {
            Explode();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = visionOrigin != null ? visionOrigin.position : transform.position;

        Vector2 forward = forwardDirection.sqrMagnitude > 0.0001f ? forwardDirection.normalized : Vector2.right;
        Vector2 leftDir = Quaternion.Euler(0, 0, viewAngleLeft) * forward;
        Vector2 rightDir = Quaternion.Euler(0, 0, -viewAngleRight) * forward;

        // Sadece koni kenarları
        Gizmos.DrawLine(origin, origin + (Vector3)(leftDir * viewDistance));
        Gizmos.DrawLine(origin, origin + (Vector3)(rightDir * viewDistance));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    #endregion
}
